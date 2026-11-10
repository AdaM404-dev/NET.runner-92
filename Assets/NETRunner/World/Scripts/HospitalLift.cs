using NetRunner.Player;
using UnityEngine;

namespace NetRunner.World
{
    [DefaultExecutionOrder(-30)]
    public sealed class HospitalLift : MonoBehaviour
    {
        enum State { Closed, Opening, Open, Closing, Moving }
        [SerializeField] StairAccess access;
        [SerializeField] FirstPersonController player;
        [SerializeField] Transform cabin;
        [SerializeField] HospitalDoor cabinDoor;
        [SerializeField] HospitalDoor[] landings;

        State state;
        int floor, destination;
        float dwell, travelSpeed;
        public int Floor => floor;
        public bool Busy => state == State.Moving || state == State.Closing;
        public bool Unlocked(int level) => level >= 0 && level <= access.Highest;

        public void Configure(StairAccess progression, FirstPersonController controller, Transform car,
            HospitalDoor carGate, HospitalDoor[] floorDoors)
        {
            access = progression;
            player = controller;
            cabin = car;
            cabinDoor = carGate;
            landings = floorDoors;
        }

        public bool Request(int target, bool fromCabin)
        {
            if (!Unlocked(target) || Busy || fromCabin && !Inside()) return false;
            if (target == floor)
            {
                OpenDoors();
                return true;
            }
            if (state == State.Opening || cabinDoor.Obstructed(player) || landings[floor].Obstructed(player))
                return false;
            destination = target;
            landings[floor].SetLiftOpen(false);
            cabinDoor.SetLiftOpen(false);
            state = State.Closing;
            return true;
        }

        bool Inside()
        {
            var p = cabin.InverseTransformPoint(player.transform.position);
            return Mathf.Abs(p.x) < 1.28f && p.z > -1.34f && p.z < 1.52f && Mathf.Abs(p.y) < .22f;
        }

        void OpenDoors()
        {
            landings[floor].SetLiftOpen(true);
            cabinDoor.SetLiftOpen(true);
            state = State.Opening;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (state == State.Opening || state == State.Closing)
            {
                if (state == State.Closing && (cabinDoor.Obstructed(player) || landings[floor].Obstructed(player)))
                {
                    destination = floor;
                    OpenDoors();
                }
                landings[floor].Advance(dt);
                cabinDoor.Advance(dt);
                if (state == State.Opening && cabinDoor.Open && landings[floor].Open)
                {
                    state = State.Open;
                    dwell = 7f;
                }
                else if (state == State.Closing && cabinDoor.Closed && landings[floor].Closed)
                    state = destination == floor ? State.Closed : State.Moving;
            }
            else if (state == State.Open)
            {
                if (cabinDoor.Obstructed(player) || landings[floor].Obstructed(player)) dwell = 7f;
                dwell -= dt;
                if (dwell <= 0f)
                {
                    destination = floor;
                    landings[floor].SetLiftOpen(false);
                    cabinDoor.SetLiftOpen(false);
                    state = State.Closing;
                }
            }
            else if (state == State.Moving)
            {
                var position = cabin.position;
                float target = destination * StairAccess.FloorHeight;
                float speed = Mathf.Min(2.25f, Mathf.Sqrt(4.2f * Mathf.Abs(target - position.y)));
                travelSpeed = Mathf.MoveTowards(travelSpeed, speed, dt * 2.1f);
                float next = Mathf.MoveTowards(position.y, target, Mathf.Max(.15f, travelSpeed) * dt);
                bool passenger = Inside();
                cabin.position = new Vector3(position.x, next, position.z);
                if (passenger) player.FollowPlatform(next - position.y);
                Physics.SyncTransforms();
                if (next == target)
                {
                    floor = destination;
                    travelSpeed = 0f;
                    OpenDoors();
                }
            }
        }
    }
}
