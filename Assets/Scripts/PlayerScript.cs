
using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plaza;
using static Plaza.InternalCalls;
using static Plaza.Input;
using static Plaza.Physics;

public class PlayerScript : Entity
{
    public float Gravity = -9.81f;
    public float TotalGravity = 0.0f;
    public float Sensitivity = 0.1f;
    public float Speed = 8.0f;
    public float JumpHeight = 40.0f;
    public float MaxSlopeAngle = 45;
    public float MoveSpeed = 38.0f;
    public float GroundDrag = 1.0f;
    public float JumpForce = 1300.0f;
    public float AirMultiplier = 1.3f;
    public bool AirJumpAvailable = true;
    public int JumpCount = 2;
    public DateTime LastJumpTime;
    public bool GamePaused = true;
    float TableHeight = 0.0f;
    List<Vector3> TablePositions = new List<Vector3>();
    List<string> ItemNames = new List<string>();

    public class Item
    {
        public UInt64 EntityUuid;
        public string Name;

    }

    public Item ItemInHand = null;//new Item();


    public int CurrentDimension = 1;

    public void SpawnTables()
    {
        Entity table = FindEntityByName("InstantiableTable");
        foreach (Vector3 position in TablePositions)
        {
            Entity newInstance = Instantiate(table);
            newInstance.GetComponent<Transform>().Translation = position;
        }
    }

    void SpawnCreatures()
    {
        Random rnd = new Random();
        List<int> alreadyGeneratedNumbers = new List<int>();
        for (int i = 0; i < 3; ++i)
        {
            Entity newCreature = Instantiate(FindEntityByName("InstantiableCreature1"));
            int number = rnd.Next(0, 18);
            while (alreadyGeneratedNumbers.Contains(number))
                number = rnd.Next(0, 18);

            alreadyGeneratedNumbers.Add(number);
            newCreature.GetComponent<Transform>().Translation = new Vector3(TablePositions[number].X, 1.0f, TablePositions[number].Z);
        }
    }

    void SpawnDirt()
    {
        int tableNumber = 5;
        Entity newDirt = Instantiate(FindEntityByName("InstantiableDirt"));
        newDirt.GetComponent<Transform>().Translation = new Vector3(TablePositions[tableNumber].X, 1.01f, TablePositions[tableNumber].Z);
    }

    public void OnStart()
    {
        //Cursor.Hide();

        Speed = 800000.0f;
        MoveSpeed = 8.0f;
        GroundDrag = 5.0f;
        AirMultiplier = 1.1f;
        JumpHeight = 20.0f;
        JumpForce = 850.0f;

        FindEntityByName("StartGui").GetComponent<GuiComponent>().Enabled = true;


        FindEntityByName("StartGui").GetComponent<GuiComponent>().FindGuiByName<GuiButton>("StartButton").Text = "Start";
        FindEntityByName("StartGui").GetComponent<GuiComponent>().FindGuiByName<GuiButton>("StartButton").LocalPosition = new Vector2(Screen.Size.X / 2.0f - 55.0f, Screen.Size.Y / 2.0f - 48.0f);
        FindEntityByName("StartGui").GetComponent<GuiComponent>().FindGuiByName<GuiButton>("StartButton").AddCallback(this.Uuid, "StartGameCallback");

        FindEntityByName("StartGui").GetComponent<GuiComponent>().FindGuiByName<GuiButton>("CloseButton").Text = "Close";
        FindEntityByName("StartGui").GetComponent<GuiComponent>().FindGuiByName<GuiButton>("CloseButton").LocalPosition = new Vector2(Screen.Size.X / 2.0f - 55.0f, Screen.Size.Y / 2.0f + 48.0f);
        FindEntityByName("StartGui").GetComponent<GuiComponent>().FindGuiByName<GuiButton>("CloseButton").AddCallback(this.Uuid, "CloseGameCallback");

        for (int i = 0; i < 3; ++i)
        {
            for (int j = 0; j < 6; ++j)
            {
                TablePositions.Add(new Vector3(18.0f + 6 * i, TableHeight, 32.0f + 6 * j));
            }
        }

        ItemNames.Add("SphereCollider");
        ItemNames.Add("InstantiableCreature1");

        SpawnTables();
        SpawnCreatures();
    }

    public void StartGameCallback()
    {
        Cursor.Hide();
        GamePaused = false;
        FindEntityByName("StartGui").GetComponent<GuiComponent>().Enabled = false;
    }

    public void CloseGameCallback()
    {
        InternalCalls.CloseGame();
    }

    public void OnUpdate()
    {
        if (Input.IsKeyDown(KeyCode.Escape))
        {
            Cursor.Show();
            GamePaused = true;
            FindEntityByName("StartGui").GetComponent<GuiComponent>().Enabled = true;
        }

        if (!GamePaused)
        {
            MovePlayer();
            RotateCamera();
            SpeedControl();
            ControlHands();
        }
    }

    public bool LastFrameEWasPressed = false;
    public void ControlHands()
    {
        if (ItemInHand == null && LastFrameEWasPressed && Input.IsKeyReleased(KeyCode.E))
        {
            // Get
            Console.WriteLine("A1");
            Physics.RaycastHit hit = Physics.Raycast(this.GetComponent<Transform>().Translation + new Vector3(0.0f, 1.0f, 0.0f), FindEntityByName("CameraEntity").GetComponent<Transform>().ForwardVector, 1000.0f);
            if (hit.hitUuid != 0)
            {
                UInt64 uuidToGet = new Entity(hit.hitUuid).Name == "SphereCollider" ? new Entity(hit.hitUuid).parent.Uuid : hit.hitUuid;
                Console.WriteLine("Hit Name: " + new Entity(hit.hitUuid).Name);
                Console.WriteLine("To Get Name: " + new Entity(uuidToGet).Name);
                Console.WriteLine(hit.hitUuid);
                Console.WriteLine(new Entity(hit.hitUuid).parent.Uuid);
                Console.WriteLine(InternalCalls.EntityGetParent(hit.hitUuid));
                
                if (new Entity(hit.hitUuid).Name == "SphereCollider")
                    new Entity(hit.hitUuid).RemoveComponent<Collider>();
                Console.WriteLine("A2");
                Console.WriteLine(uuidToGet);
                Console.WriteLine(new Entity(uuidToGet).Name);
                Console.WriteLine("A3");
                if (ItemNames.Contains(new Entity(uuidToGet).Name))
                {
                    Console.WriteLine("A4");
                    ItemInHand = new Item();
                    ItemInHand.EntityUuid = uuidToGet;
                    ItemInHand.Name = new Entity(uuidToGet).Name;
                    Console.WriteLine("A5");
                    new Entity(uuidToGet).parent = this;
                    new Entity(uuidToGet).GetComponent<Transform>().Translation = new Vector3(0.0f, 0.5f, 1.0f);
                }
            }
        }
        else if (LastFrameEWasPressed && Input.IsKeyReleased(KeyCode.E))
        {
            //Drop
            new Entity(ItemInHand.EntityUuid).parent = FindEntityByName("Scene");
            new Entity(ItemInHand.EntityUuid).GetComponent<Transform>().Translation = FindEntityByName("Body").GetComponent<Transform>().Translation;
        }

        if (Input.IsKeyDown(KeyCode.E))
        {
            LastFrameEWasPressed = true;
        }
        else
            LastFrameEWasPressed = false;
    }

    public void MovePlayer()
    {
        Vector3 force = new Vector3(0.0f);
        float vertical = 0.0f;
        float horizontal = 0.0f;
        Vector3 movement = new Vector3(0.0f, TotalGravity, 0.0f);

        Transform transform = this.GetComponent<Transform>();

        RaycastHit hit;
        Physics_Raycast(this.GetComponent<Transform>().Translation, new Vector3(0.0f, -1.0f, 0.0f), 0.7f, out hit, this.Uuid);

        bool hittingGround = hit.hitUuid != 0;

        if (Input.IsKeyDown(KeyCode.W))
        {
            horizontal += Speed * Time.deltaTime;
        }
        if (Input.IsKeyDown(KeyCode.S))
        {
            horizontal -= Speed * Time.deltaTime;
        }
        if (Input.IsKeyDown(KeyCode.A))
        {
            vertical += Speed * Time.deltaTime;
        }
        if (Input.IsKeyDown(KeyCode.D))
        {
            vertical -= Speed * Time.deltaTime;
        }

        force = this.GetComponent<Transform>().LeftVector * -vertical + this.GetComponent<Transform>().ForwardVector * horizontal;
        force += new Vector3(0.0f, TotalGravity, 0.0f);
        //force *= Time.deltaTime * 100.0f;

        if (OnSlope(hit))
        {
            this.GetComponent<RigidBody>().AddForce(GetSlopeMoveDirection(hit, force), ForceMode.FORCE);
        }
        else
        {
            if (hittingGround)
                this.GetComponent<RigidBody>().AddForce(force, ForceMode.FORCE);
            else
                this.GetComponent<RigidBody>().AddForce(force * AirMultiplier, ForceMode.FORCE);
        }

        if (hittingGround)
        {
            TotalGravity = Gravity;
            this.GetComponent<RigidBody>().drag = GroundDrag;

            AirJumpAvailable = true;
        }
        else
        {
            this.GetComponent<RigidBody>().drag = 0.0f;
            TotalGravity += Gravity;
        }

        HandleJump(hittingGround);

    }

    public void HandleJump(bool hittingGround)
    {
        bool isJumpCoolDownOver = (LastJumpTime - DateTime.Now).TotalSeconds < -0.4f;
        if (isJumpCoolDownOver && Input.IsKeyDown(KeyCode.Space) && hittingGround)
        {
            //this.GetComponent<RigidBody>().ApplyForce(new Vector3(0.0f, jumpHeight, 0.0f));
            Jump();
            LastJumpTime = DateTime.Now;
        }
    }

    public void Jump()
    {
        TotalGravity = 0.0f;
        RigidBody rigidBody = this.GetComponent<RigidBody>();
        rigidBody.velocity = new Vector3(rigidBody.velocity.X, 0.0f, rigidBody.velocity.Z);

        rigidBody.velocity = new Vector3(rigidBody.velocity.X, 0.0f, rigidBody.velocity.Z);
        rigidBody.ApplyForce(new Vector3(0.0f, 0.0f, 0.0f));
        rigidBody.AddForce(new Vector3(0.0f, 1.0f, 0.0f) * JumpForce, ForceMode.IMPULSE);

    }

    public void SpeedControl()
    {
        Vector3 rigidBodyVelocity = this.GetComponent<RigidBody>().velocity;
        Vector3 flatVelocity = new Vector3(rigidBodyVelocity.X, 0.0f, rigidBodyVelocity.Z);
        if (Vector3.Magnitude(flatVelocity) > MoveSpeed)
        {
            Vector3 limitedVelocity = Vector3.Normalize(flatVelocity) * MoveSpeed;
            this.GetComponent<RigidBody>().velocity = new Vector3(limitedVelocity.X, rigidBodyVelocity.Y, limitedVelocity.Z);
        }
    }

    private bool OnSlope(RaycastHit hit)
    {
        if (hit.hitUuid == 0)
            return false;
        float angle = Vector3.Angle(new Vector3(0.0f, 1.0f, 0.0f), hit.normal);
        return angle < MaxSlopeAngle && angle != 0;
    }

    private Vector3 GetSlopeMoveDirection(RaycastHit hit, Vector3 moveDirection)
    {
        return Vector3.ProjectOnPlane(moveDirection, hit.normal);
    }

    public void RotateCamera()
    {
        Quaternion quat = new Quaternion(new Vector3(0.0f, -Input.MouseDeltaX() * Sensitivity * 0.01f, 0.0f));
        this.GetComponent<Transform>().Rotation *= quat;

        Quaternion newRotation = FindEntityByName("CameraEntity").GetComponent<Transform>().Rotation * new Quaternion(new Vector3(Input.MouseDeltaY() * Sensitivity * 0.01f, 0.0f, 0.0f));
        newRotation.z = Clamp(newRotation.z, -45.0f * Mathf.Deg2Rad, 45.0f * Mathf.Deg2Rad);

        FindEntityByName("CameraEntity").GetComponent<Transform>().Rotation = newRotation;
    }

    public static float Clamp(float angle, float minAngle, float maxAngle)
    {
        return Math.Min(Math.Max(angle, minAngle), maxAngle);
    }
}
