using NetRunner.Player;
using NetRunner.SceneBuilding.Editor;
using UnityEngine;

namespace NetRunner.World.Editor
{
    /// <summary>
    /// Builds the west passenger lift: the moving cabin with its walls,
    /// luminaire and light, the cabin's sliding doors, a call button on each
    /// landing and a floor button for each level inside, all driven by
    /// <see cref="HospitalLift"/> with the four landing doors from the model.
    /// </summary>
    public static class HospitalLiftBuilder
    {
        private const int Levels = 4;

        // The cabin's light: a short-range cool practical under the luminaire.
        private const float CabinLightRange = 3.4f;
        private const float CabinLightIntensity = 135f;

        // The cabin doors: two 86 cm leaves that part from the centre to 1.28 m.
        private const float CabinDoorWidth = 1.72f;
        private const float CabinDoorZ = 1.59f;

        // Buttons: landing call buttons by the doors, cabin buttons in a column, 19 cm apart.
        private const float CallButtonHeight = 1.28f;
        private const float CabinButtonHeight = 1.27f;
        private const float CabinButtonSpacing = .19f;

        private static readonly Vector3 CabinStart = new Vector3(-2f, 0f, 7.87f);
        private static readonly Vector3 CabinLightPosition = new Vector3(0, 2.37f, .05f);
        private static readonly Color CabinLightColor = new Color(.72f, .87f, 1f);
        private static readonly Color KeypadColor = new Color(.25f, .45f, .5f);
        private static readonly Color KeypadGlow = new Color(.24f, .72f, .85f) * 2f;
        private const float KeypadMetallic = .14f;
        private static readonly float[] Sides = { -1f, 1f };

        /// <summary>Adds the lift under <paramref name="systems"/>, finished in <paramref name="metal"/> and <paramref name="edge"/>.</summary>
        public static void Add(Transform systems, FirstPersonController player, StairAccess access, HospitalDoor[] landings,
            Material metal, Material edge)
        {
            Transform root = SceneParts.Node("West passenger lift", systems);
            HospitalLift lift = root.gameObject.AddComponent<HospitalLift>();
            Transform car = SceneParts.Node("Moving cabin", root);
            car.position = CabinStart;
            Material keypad = SceneParts.SystemMaterial("Lift keypad and luminaire", KeypadColor, KeypadMetallic, KeypadGlow);
            AddCabin(car, metal, edge, keypad);
            HospitalDoor carDoor = AddCabinDoor(car, metal);
            lift.Configure(access, player, car, carDoor, landings);
            for (int i = 0; i < Levels; i++)
            {
                float y = i * StairAccess.FloorHeight;
                Transform call = SceneParts.Block(root, $"West lift call L{i + 1}", new Vector3(-.88f, y + CallButtonHeight, 5.98f),
                    new Vector3(.24f, .28f, .12f), keypad);
                call.gameObject.AddComponent<HospitalLiftButton>().Configure(lift, i, false);
                Transform button = SceneParts.Block(car, $"Cabin level {i + 1}",
                    new Vector3(.91f, CabinButtonHeight + i * CabinButtonSpacing, 1.53f), new Vector3(.17f, .14f, .075f), keypad);
                button.gameObject.AddComponent<HospitalLiftButton>().Configure(lift, i, true);
            }
        }

        private static void AddCabin(Transform car, Material metal, Material edge, Material keypad)
        {
            SceneParts.Block(car, "Cabin deck", new Vector3(0, -.075f, -.01f), new Vector3(2.64f, .15f, 3.35f), edge);
            SceneParts.Block(car, "Cabin ceiling", new Vector3(0, 2.56f, 0), new Vector3(2.64f, .12f, 3.34f), metal);
            SceneParts.Block(car, "Cabin rear", new Vector3(0, 1.25f, 1.62f), new Vector3(2.63f, 2.56f, .12f), metal);
            foreach (float side in Sides)
            {
                SceneParts.Block(car, "Cabin side wall", new Vector3(side * 1.26f, 1.25f, 0), new Vector3(.12f, 2.55f, 3.26f), metal);
                SceneParts.Block(car, "Front return", new Vector3(side * 1.065f, 1.26f, -1.56f), new Vector3(.40f, 2.52f, .10f), metal);
            }

            SceneParts.Block(car, "Front header", new Vector3(0, 2.41f, -1.56f), new Vector3(1.77f, .22f, .10f), edge);
            SceneParts.Block(car, "Cabin luminaire", new Vector3(0, 2.48f, .10f), new Vector3(.52f, .045f, 1.28f), keypad, false);
            Transform practical = SceneParts.Node("Cabin practical", car);
            practical.localPosition = CabinLightPosition;
            Light light = practical.gameObject.AddComponent<Light>();
            light.range = CabinLightRange;
            light.intensity = CabinLightIntensity;
            light.color = CabinLightColor;
        }

        private static HospitalDoor AddCabinDoor(Transform car, Material metal)
        {
            Transform door = SceneParts.Node("Cabin interlock", car);
            door.localPosition = Vector3.back * CabinDoorZ;
            var panels = new HospitalDoor.Panel[Sides.Length];
            for (int i = 0; i < Sides.Length; i++)
            {
                float side = Sides[i];
                Transform panel = SceneParts.Block(door, "Cabin sliding leaf", Vector3.zero, new Vector3(.86f, 2.30f, .052f), metal);
                panels[i] = new HospitalDoor.Panel(panel, new Vector3(side * .43f, 1.16f, 0), new Vector3(side * 1.28f, 1.16f, .11f),
                    Quaternion.identity, Quaternion.identity);
            }

            HospitalDoor carDoor = door.gameObject.AddComponent<HospitalDoor>();
            carDoor.Configure(panels, CabinDoorWidth, false, true, false);
            return carDoor;
        }
    }
}
