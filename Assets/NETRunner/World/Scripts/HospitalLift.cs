using NetRunner.Core;
using NetRunner.Player;
using UnityEngine;

namespace NetRunner.World
{
    /// <summary>
    /// The west lift: a cabin with its own door and a landing door on each
    /// floor. Core's <see cref="LiftCar"/> decides what it does; this component
    /// moves the doors and the cabin, carries a passenger along, and tells the
    /// car whether someone stands in a doorway and whether the doors are
    /// fully open or shut. Only floors the stairs have reached
    /// (<see cref="StairAccess"/>) can be chosen.
    /// </summary>
    [DefaultExecutionOrder(-30)]
    public sealed class HospitalLift : MonoBehaviour
    {
        [SerializeField] private StairAccess access;
        [SerializeField] private FirstPersonController player;
        [SerializeField] private Transform cabin;
        [SerializeField] private HospitalDoor cabinDoor;
        [SerializeField] private HospitalDoor[] landings;

        private readonly LiftCar car = new LiftCar();

        /// <summary>The floor the cabin is at or last left (0-based).</summary>
        public int Floor => car.Floor;

        /// <summary>Whether the lift is shutting its doors or travelling, and takes no requests.</summary>
        public bool Busy => car.Busy;

        private void Awake()
        {
            car.DoorsCommanded += CommandDoors;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            switch (car.Current)
            {
                case LiftCar.State.Opening:
                case LiftCar.State.Closing:
                    if (car.Current == LiftCar.State.Closing && DoorwayBlocked())
                    {
                        car.Reopen();
                    }

                    HospitalDoor landing = landings[car.Floor];
                    landing.Advance(dt);
                    cabinDoor.Advance(dt);
                    car.DoorsMoved(cabinDoor.Open && landing.Open, cabinDoor.Closed && landing.Closed);
                    break;
                case LiftCar.State.Open:
                    car.Wait(dt, DoorwayBlocked());
                    break;
                case LiftCar.State.Moving:
                    Ride(dt);
                    break;
            }
        }

        /// <summary>Sets the stairs, the player, the cabin and the doors; the scene builder calls it.</summary>
        public void Configure(StairAccess progression, FirstPersonController controller, Transform car,
            HospitalDoor carGate, HospitalDoor[] floorDoors)
        {
            access = progression;
            player = controller;
            cabin = car;
            cabinDoor = carGate;
            landings = floorDoors;
        }

        /// <summary>Whether <paramref name="level"/> can be chosen: the stairs have reached it.</summary>
        public bool Unlocked(int level)
        {
            return LiftCar.IsReached(level, access.Highest);
        }

        /// <summary>
        /// A call button (<paramref name="fromCabin"/> false) or a cabin button
        /// asks for <paramref name="target"/>; returns whether the lift accepted.
        /// </summary>
        public bool Request(int target, bool fromCabin)
        {
            return car.Request(target, access.Highest, fromCabin, fromCabin && Inside(), DoorwayBlocked());
        }

        private bool Inside()
        {
            Vector3 local = cabin.InverseTransformPoint(player.transform.position);
            return LiftCar.Carries(local.x, local.y, local.z);
        }

        private bool DoorwayBlocked()
        {
            return cabinDoor.Obstructed(player) || landings[car.Floor].Obstructed(player);
        }

        private void CommandDoors(bool open)
        {
            landings[car.Floor].SetLiftOpen(open);
            cabinDoor.SetLiftOpen(open);
        }

        // Moves the cabin for one frame and carries a passenger with it.
        private void Ride(float dt)
        {
            Vector3 position = cabin.position;
            float next = car.Travel(position.y, dt);
            bool passenger = Inside();
            cabin.position = new Vector3(position.x, next, position.z);
            if (passenger)
            {
                player.FollowPlatform(next - position.y);
            }

            Physics.SyncTransforms();
        }
    }
}
