#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NetRunner.Enemy;
using NetRunner.Player;
using NetRunner.World;
using UnityEngine;

namespace NetRunner.Diagnostics
{
    /// <summary>The hospital's systems: doors, stairs, the lift, the K-7 patrols and residual power.</summary>
    public sealed partial class ExplorationDiagnostics
    {
        private const string SystemsRoot = "Hospital systems — circulation and patrols";
        private const string DoorsGroup = "Doors";
        private const int ExpectedDoors = 108;
        private const int Patrols = 4;

        // How long the K-7s get to show they walk: longer than a robot's wait at
        // a route end (Patrol.EndPause, 1.15 s) plus its turn on the spot.
        private const float PatrolTimeout = 6f;

        private static HospitalDoor[] ArchitectureDoors()
        {
            return GameObject.Find(SystemsRoot).transform.Find(DoorsGroup).GetComponentsInChildren<HospitalDoor>();
        }

        private void TestClosedSystems()
        {
            HospitalDoor[] doors = ArchitectureDoors();
            int blocked = 0;
            foreach (HospitalDoor door in doors)
            {
                Transform frame = door.transform;
                Vector3 start = frame.position + Vector3.up * 1.3f - frame.forward * .72f;
                if (Physics.Raycast(start, frame.forward, out RaycastHit hit, 1.42f, DefaultLayer, QueryTriggerInteraction.Ignore)
                    && hit.collider.GetComponentInParent<HospitalDoor>() == door)
                {
                    blocked++;
                }
            }

            Check(doors.Length == ExpectedDoors && doors.All(d => d.Closed) && blocked == doors.Length,
                $"{doors.Length} door assemblies start closed. {blocked} door centers have a physical blocker.");
            HospitalLift lift = FindAnyObjectByType<HospitalLift>();
            Check(lift && lift.Unlocked(0) && !lift.Request(1, false) && !lift.Request(3, true),
                "Lift rejects undiscovered floors before the stairs are used.");
        }

        private IEnumerator TestStairsAndLift(StairAccess access)
        {
            player.Warp(new Vector3(0, .03f, 0), Quaternion.identity);
            for (int leg = 0; leg < 3; leg++)
            {
                int core = StairAccess.CoreForLeg(leg);
                Check(Connect(core, leg, true), $"Required L{leg + 1} to L{leg + 2} leg uses the {(core < 0 ? "west" : "east")} core.");
                yield return null;
                yield return null;
                Check(access.Highest == leg + 1, $"Exited the active stair onto L{leg + 2} and unlocked its lift stop.");
            }

            for (int leg = 2; leg >= 0; leg--)
            {
                Check(Connect(StairAccess.CoreForLeg(leg), leg, false), $"Required return L{leg + 2} to L{leg + 1} stair.");
            }

            TestFireIsolation();
            yield return TestLift();
        }

        // Every flight not on the first ascent is shut at both ends: a sprint
        // into it from either landing stops before the flight.
        private void TestFireIsolation()
        {
            for (int leg = 0; leg < 3; leg++)
            {
                int core = -StairAccess.CoreForLeg(leg);
                for (int end = 0; end < 2; end++)
                {
                    float sign = end == 0 ? -1f : 1f;
                    float y = (leg + end) * StairAccess.FloorHeight;
                    player.Warp(new Vector3(core + sign * 1.185f, y + .04f, -4.42f), Quaternion.identity);
                    for (int step = 0; step < 115; step++)
                    {
                        player.Move(Vector3.back, true, Step);
                    }

                    Check(player.transform.position.z > -5.30f && Mathf.Abs(player.transform.position.y - y) < .26f,
                        $"Isolated L{leg + 1}–L{leg + 2} flight blocks {(end == 0 ? "ascent" : "descent")}.");
                }
            }
        }

        // Calls the lift from the lobby, rides it to every floor and back with
        // the cabin buttons, and walks out again.
        private IEnumerator TestLift()
        {
            HospitalLift lift = FindAnyObjectByType<HospitalLift>();
            player.Warp(new Vector3(-.88f, .034f, 4.55f), Quaternion.identity);
            Physics.SyncTransforms();
            HospitalInteraction interaction = player.GetComponent<HospitalInteraction>();
            var call = interaction.TargetInView() as HospitalLiftButton;

            // The call control is chest-high, so aim slightly down from eye height.
            if (!call)
            {
                player.Look(new Vector2(0, -82));
                call = interaction.TargetInView() as HospitalLiftButton;
            }

            Check(call, "West lift call button can be targeted from the lobby.");
            if (call)
            {
                call.Interact(player);
            }
            else
            {
                lift.Request(0, false);
            }

            yield return new WaitForSeconds(.82f);
            bool hasEntered = Travel("Lift cabin boarding", new[] { P(-2f, 5.45f), P(-2f, 7.25f) });
            Check(hasEntered && player.IsGrounded && Mathf.Abs(player.transform.position.y) < .14f, "Player walked into the cabin and stood on the deck.");
            if (!hasEntered)
            {
                yield break;
            }

            for (int i = 0; i < 4; i++)
            {
                int target = i == 3 ? 0 : i + 1;
                HospitalLiftButton button = GameObject.Find($"Cabin level {target + 1}").GetComponent<HospitalLiftButton>();
                Transform eye = player.Eyes.transform;
                Vector3 angle = Quaternion.LookRotation(button.transform.position - eye.position).eulerAngles;
                player.Look(new Vector2(Mathf.DeltaAngle(eye.eulerAngles.y, angle.y), Mathf.DeltaAngle(angle.x, eye.eulerAngles.x)) / LookSensitivity);
                Physics.SyncTransforms();
                bool isAimed = interaction.TargetInView() as UnityEngine.Object == button;
                Check(isAimed, $"Cabin level {target + 1} button is reachable from inside.");
                if (isAimed)
                {
                    button.Interact(player);
                }
                else
                {
                    lift.Request(target, true);
                }

                float end = Time.realtimeSinceStartup + 12f;
                while (lift.Floor != target && Time.realtimeSinceStartup < end)
                {
                    player.Move(Vector3.zero, false, Time.deltaTime);
                    yield return null;
                }

                yield return new WaitForSeconds(.84f);
                Check(lift.Floor == target && Mathf.Abs(player.transform.position.y - target * StairAccess.FloorHeight) < .13f,
                    $"Cabin transported the actual player capsule to L{target + 1}.");
            }

            Check(Travel("Lift cabin exit", new[] { P(-2f, 5.44f), P(-1f, 4.35f) }) && player.IsGrounded,
                "Player walked back out of the lift onto the lobby slab.");
            HospitalDoor[] west = ArchitectureDoors().Where(d => d.LiftOnly && d.transform.position.x < 0).ToArray();
            Check(west.Count(d => d.Open) == 1 && west.Where(d => d.Open).All(d => d.transform.position.y < .1f),
                "Only the landing aligned with the cabin opens.");
        }

        private void OpenRoomDoorsForSweep()
        {
            HospitalDoor[] doors = ArchitectureDoors().Where(d => !d.LiftOnly).ToArray();
            foreach (HospitalDoor door in doors)
            {
                if (!door.Open)
                {
                    door.Interact(player);
                }

                door.Advance(1f);
            }

            Physics.SyncTransforms();
            Check(doors.All(d => d.Open), $"All {doors.Length} standard doors can open for the room clearance sweep.");
        }

        private IEnumerator TestPatrols()
        {
            K7Patrol[] patrols = FindObjectsByType<K7Patrol>().OrderBy(p => p.name).ToArray();
            int free = patrols.Count(IsRouteClear);
            Check(free == Patrols, $"{free} K-7 routes have unobstructed body clearance.");
            Vector3[] before = patrols.Select(p => p.transform.position).ToArray();
            Transform[] legs = patrols.Select(p => p.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Thigh_L")).ToArray();
            Quaternion[] pose = legs.Select(t => t ? t.localRotation : Quaternion.identity).ToArray();

            // Each robot counts once it has moved 10 cm and turned its left
            // thigh, so one waiting at its route end gets the time to walk on.
            var hasWalked = new bool[patrols.Length];
            float deadline = Time.time + PatrolTimeout;
            while (hasWalked.Any(walked => !walked) && Time.time < deadline)
            {
                yield return null;
                for (int i = 0; i < patrols.Length; i++)
                {
                    hasWalked[i] |= Vector3.Distance(before[i], patrols[i].transform.position) > .1f && legs[i]
                        && Quaternion.Angle(pose[i], legs[i].localRotation) > .25f;
                }
            }

            int moving = hasWalked.Count(walked => walked);
            Check(patrols.Length == Patrols && moving == Patrols && patrols.All(p =>
                    p.GetComponentInChildren<LODGroup>()?.lodCount == 4 && p.GetComponentInChildren<IInteractable>() == null),
                $"{moving} K-7 patrols moved and animated their original rig. All have four LODs and no interaction target.");
            if (patrols.Length > 1)
            {
                yield return TestSensor(patrols[1]);
            }
        }

        // Whether the robot's body fits along its whole route: a capsule every 25 cm.
        private static bool IsRouteClear(K7Patrol patrol)
        {
            for (int i = 1; i < patrol.Waypoints.Count; i++)
            {
                Vector3 a = patrol.Waypoints[i - 1];
                Vector3 b = patrol.Waypoints[i];
                int samples = Mathf.CeilToInt(Vector3.Distance(a, b) * 4f);
                for (int j = 0; j <= samples; j++)
                {
                    Vector3 p = Vector3.Lerp(a, b, (float)j / samples);
                    if (Physics.CheckCapsule(p + Vector3.up * .35f, p + Vector3.up * 1.65f, .32f, DefaultLayer, QueryTriggerInteraction.Ignore))
                    {
                        Debug.LogWarning($"NET92 K7 blocked route {patrol.name}: {p}");
                        return false;
                    }
                }
            }

            return true;
        }

        // Stops the L2 robot in the ward and checks its sensor's height and direction, with a picture.
        private IEnumerator TestSensor(K7Patrol robot)
        {
            robot.enabled = false;
            Transform patrol = robot.transform;
            patrol.SetPositionAndRotation(new Vector3(-7.25f, 4.215f, -.72f), Quaternion.Euler(0, 90f, 0));
            Transform[] rig = patrol.GetComponentsInChildren<Transform>();
            Transform head = rig.First(t => t.name == "Head");
            Transform sensor = rig.First(t => t.name == "VisionOrigin");
            float reach = Vector3.Dot(sensor.position - head.position, patrol.forward);
            float headHeight = sensor.position.y - patrol.position.y;
            Check(headHeight > 1.8f && headHeight < 2.1f && reach > .08f,
                $"K-7 sensor is {headHeight:F2} m above the floor and faces the patrol direction.");
            yield return Capture("07_K7_Ward_Patrol", new Vector3(-3.15f, 4.234f, -.72f), 270f, true);
            robot.enabled = true;
        }

        // Four failing lights must dip below 10% and come back within 12.6 s;
        // five more readouts flicker on their own.
        private IEnumerator TrackResidualPower()
        {
            ResidualPower[] power = FindObjectsByType<ResidualPower>();
            Light[] lamps = power.Select(p => p.DiagnosticFixture).Where(l => l).ToArray();
            float[] low = lamps.Select(l => l.intensity).ToArray();
            float[] high = (float[])low.Clone();
            float deadline = Time.time + 12.6f;
            while (Time.time < deadline)
            {
                for (int i = 0; i < lamps.Length; i++)
                {
                    low[i] = Mathf.Min(low[i], lamps[i].intensity);
                    high[i] = Mathf.Max(high[i], lamps[i].intensity);
                }

                yield return null;
            }

            int floors = lamps.Select(l => Mathf.RoundToInt(l.transform.position.y / StairAccess.FloorHeight)).Distinct().Count();
            Check(lamps.Length == 4 && lamps.Distinct().Count() == 4 && floors == 4
                && low.Where((value, i) => value <= high[i] * .1f).Count() == 4 && power.Length - lamps.Length == 5,
                "Four practicals, one per floor, dropped below 10% and recovered. Five additional readout power loops are present.");
        }
    }
}
#endif
