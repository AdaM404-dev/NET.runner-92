#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NetRunner.Enemy;
using NetRunner.Player;
using NetRunner.World;
using UnityEngine;

namespace NetRunner.Diagnostics
{
    public sealed partial class ExplorationDiagnostics
    {
        static HospitalDoor[] ArchitectureDoors() => GameObject.Find("Hospital systems — circulation and patrols")
            .transform.Find("Doors").GetComponentsInChildren<HospitalDoor>();

        void TestClosedSystems()
        {
            var doors = ArchitectureDoors();
            int blocked = 0;
            foreach (var door in doors)
            {
                var t = door.transform;
                Vector3 start = t.position + Vector3.up * 1.3f - t.forward * .72f;
                if (Physics.Raycast(start, t.forward, out var hit, 1.42f, 1, QueryTriggerInteraction.Ignore)
                    && hit.collider.GetComponentInParent<HospitalDoor>() == door) blocked++;
            }
            Check(doors.Length == 108 && doors.All(d => d.Closed) && blocked == doors.Length,
                $"{doors.Length} door assemblies start closed. {blocked} door centers have a physical blocker.");
            var lift = FindAnyObjectByType<HospitalLift>();
            Check(lift && lift.Unlocked(0) && !lift.Request(1, false) && !lift.Request(3, true),
                "Lift rejects undiscovered floors before the stairs are used.");
        }

        IEnumerator TestStairsAndLift(StairAccess access)
        {
            player.Warp(new Vector3(0,.03f,0), Quaternion.identity);
            for (int leg = 0; leg < 3; leg++)
            {
                int core = StairAccess.CoreForLeg(leg);
                Check(Connect(core, leg, true), $"Required L{leg + 1} to L{leg + 2} leg uses the {(core < 0 ? "west" : "east")} core.");
                yield return null;
                yield return null;
                Check(access.Highest == leg + 1, $"Exited the active stair onto L{leg + 2} and unlocked its lift stop.");
            }
            for (int leg = 2; leg >= 0; leg--) Check(Connect(StairAccess.CoreForLeg(leg), leg, false), $"Required return L{leg + 2} to L{leg + 1} stair.");
            for (int leg = 0; leg < 3; leg++)
            {
                int core = -StairAccess.CoreForLeg(leg);
                for (int end = 0; end < 2; end++)
                {
                    float sign = end == 0 ? -1f : 1f;
                    float y = (leg + end) * StairAccess.FloorHeight;
                    player.Warp(new Vector3(core + sign * 1.185f, y + .04f, -4.42f), Quaternion.identity);
                    for (int step = 0; step < 115; step++) player.Move(Vector3.back, true, 1f / 90f);
                    Check(player.transform.position.z > -5.30f && Mathf.Abs(player.transform.position.y - y) < .26f,
                        $"Isolated L{leg + 1}–L{leg + 2} flight blocks {(end == 0 ? "ascent" : "descent")}.");
                }
            }

            var lift = FindAnyObjectByType<HospitalLift>();
            player.Warp(new Vector3(-.88f, .034f, 4.55f), Quaternion.identity);
            Physics.SyncTransforms();
            var call = player.GetComponent<HospitalInteraction>().TargetInView() as HospitalLiftButton;
            // The call control is chest-high, so aim slightly down from eye height.
            if (!call)
            {
                player.Look(new Vector2(0, -82));
                call = player.GetComponent<HospitalInteraction>().TargetInView() as HospitalLiftButton;
            }
            Check(call, "West lift call button can be targeted from the lobby.");
            if (call) call.Interact(player);
            else lift.Request(0, false);
            yield return new WaitForSeconds(.82f);
            bool entered = Travel("Lift cabin boarding", new[] { P(-2f, 5.45f), P(-2f, 7.25f) });
            Check(entered && player.IsGrounded && Mathf.Abs(player.transform.position.y) < .14f, "Player walked into the cabin and stood on the deck.");
            if (!entered) yield break;
            for (int i = 0; i < 4; i++)
            {
                int target = i == 3 ? 0 : i + 1;
                var button = GameObject.Find($"Cabin level {target + 1}").GetComponent<HospitalLiftButton>();
                var eye = player.Eyes.transform;
                var angle = Quaternion.LookRotation(button.transform.position - eye.position).eulerAngles;
                player.Look(new Vector2(Mathf.DeltaAngle(eye.eulerAngles.y, angle.y), Mathf.DeltaAngle(angle.x, eye.eulerAngles.x)) / .13f);
                Physics.SyncTransforms();
                bool aimed = player.GetComponent<HospitalInteraction>().TargetInView() as UnityEngine.Object == button;
                Check(aimed, $"Cabin level {target + 1} button is reachable from inside.");
                if (aimed) button.Interact(player);
                else lift.Request(target, true);
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
            var west = ArchitectureDoors().Where(d => d.LiftOnly && d.transform.position.x < 0).ToArray();
            Check(west.Count(d => d.Open) == 1 && west.Where(d => d.Open).All(d => d.transform.position.y < .1f),
                "Only the landing aligned with the cabin opens.");
        }

        void OpenRoomDoorsForSweep()
        {
            var doors = ArchitectureDoors().Where(d => !d.LiftOnly).ToArray();
            foreach (var door in doors)
            {
                if (!door.Open) door.Interact(player);
                door.Advance(1f);
            }
            Physics.SyncTransforms();
            Check(doors.All(d => d.Open), $"All {doors.Length} standard doors can open for the room clearance sweep.");
        }

        IEnumerator TestPatrols()
        {
            var patrols = FindObjectsByType<K7Patrol>(FindObjectsSortMode.None).OrderBy(p => p.name).ToArray();
            int free = 0;
            foreach (var patrol in patrols)
            {
                bool clear = true;
                for (int i = 1; i < patrol.Waypoints.Count && clear; i++)
                {
                    Vector3 a = patrol.Waypoints[i - 1], b = patrol.Waypoints[i];
                    int samples = Mathf.CeilToInt(Vector3.Distance(a, b) * 4f);
                    for (int j = 0; j <= samples; j++)
                    {
                        Vector3 p = Vector3.Lerp(a, b, (float)j / samples);
                        if (!Physics.CheckCapsule(p + Vector3.up * .35f, p + Vector3.up * 1.65f, .32f, 1, QueryTriggerInteraction.Ignore)) continue;
                        Debug.LogWarning($"NET92 K7 blocked route {patrol.name}: {p}");
                        clear = false;
                        break;
                    }
                }
                if (clear) free++;
            }
            Check(free == 4, $"{free} K-7 routes have unobstructed body clearance.");
            var before = patrols.Select(p => p.transform.position).ToArray();
            var legs = patrols.Select(p => p.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Thigh_L")).ToArray();
            var pose = legs.Select(t => t ? t.localRotation : Quaternion.identity).ToArray();
            yield return new WaitForSeconds(2.42f);
            int moving = 0;
            for (int i = 0; i < patrols.Length; i++)
                if (Vector3.Distance(before[i], patrols[i].transform.position) > .1f && legs[i]
                    && Quaternion.Angle(pose[i], legs[i].localRotation) > .25f) moving++;
            Check(patrols.Length == 4 && moving == 4 && patrols.All(p =>
                p.GetComponentInChildren<LODGroup>()?.lodCount == 4 && p.GetComponentInChildren<IInteractable>() == null),
                $"{moving} K-7 patrols moved and animated their original rig. All have four LODs and no interaction target.");
            if (patrols.Length > 1)
            {
                patrols[1].enabled = false;
                var patrol = patrols[1].transform;
                patrol.SetPositionAndRotation(new Vector3(-7.25f, 4.215f, -.72f), Quaternion.Euler(0, 90f, 0));
                var rig = patrol.GetComponentsInChildren<Transform>();
                var head = rig.First(t => t.name == "Head");
                var sensor = rig.First(t => t.name == "VisionOrigin");
                float reach = Vector3.Dot(sensor.position - head.position, patrol.forward);
                float headHeight = sensor.position.y - patrol.position.y;
                Check(headHeight > 1.8f && headHeight < 2.1f && reach > .08f,
                    $"K-7 sensor is {headHeight:F2} m above the floor and faces the patrol direction.");
                yield return Capture("07_K7_Ward_Patrol", new Vector3(-3.15f, 4.234f, -.72f), 270f, true);
                patrols[1].enabled = true;
            }
        }

        IEnumerator TrackResidualPower()
        {
            var power = FindObjectsByType<ResidualPower>(FindObjectsSortMode.None);
            var lamps = power.Select(p => p.DiagnosticFixture).Where(l => l).ToArray();
            float[] low = lamps.Select(l => l.intensity).ToArray(), high = (float[])low.Clone();
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
            Check(lamps.Length == 4 && low.Where((value, i) => value <= high[i] * .1f).Count() == 4
                && power.Length - lamps.Length == 5,
                "Four practicals dropped below 10% and recovered. Five additional readout power loops are present.");
        }

    }
}
#endif
