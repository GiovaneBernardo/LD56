
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

    public Transform Box1Transform = null;
    public Transform Box2Transform = null;

    public static List<UInt64> _ButtonsUuids = new List<UInt64>();
    public static List<UInt64> _ActiveButtons = new List<UInt64>();

    public int CurrentDimension = 1;
    public void OnStart()
    {
        //Cursor.Hide();

        Speed = 800000.0f;
        MoveSpeed = 8.0f;
        GroundDrag = 5.0f;
        AirMultiplier = 1.1f;
        JumpHeight = 20.0f;
        JumpForce = 850.0f;

        _ButtonsUuids.Add(FindEntityByName("Button1").Uuid);
        _ButtonsUuids.Add(FindEntityByName("Button2").Uuid);

        Box1Transform = FindEntityByName("Box1").GetComponent<Transform>();
        Box2Transform = FindEntityByName("Box2").GetComponent<Transform>();

        FindEntityByName("StartGui").GetComponent<GuiComponent>().Enabled = true;


        FindEntityByName("StartGui").GetComponent<GuiComponent>().FindGuiByName<GuiButton>("StartButton").Text = "Start";
        FindEntityByName("StartGui").GetComponent<GuiComponent>().FindGuiByName<GuiButton>("StartButton").LocalPosition = new Vector2(Screen.Size.X / 2.0f - 55.0f, Screen.Size.Y / 2.0f - 48.0f);
        FindEntityByName("StartGui").GetComponent<GuiComponent>().FindGuiByName<GuiButton>("StartButton").AddCallback(this.Uuid, "StartGameCallback");

        FindEntityByName("StartGui").GetComponent<GuiComponent>().FindGuiByName<GuiButton>("CloseButton").Text = "Close";
        FindEntityByName("StartGui").GetComponent<GuiComponent>().FindGuiByName<GuiButton>("CloseButton").LocalPosition = new Vector2(Screen.Size.X / 2.0f - 55.0f, Screen.Size.Y / 2.0f + 48.0f);
        FindEntityByName("StartGui").GetComponent<GuiComponent>().FindGuiByName<GuiButton>("CloseButton").AddCallback(this.Uuid, "CloseGameCallback");
        //Mesh mesh = new Mesh();
        //for (int i = 0; i < 100; ++i)
        //{
        //    for (int j = 0; j < 100; ++j)
        //    {
        //
        //    }
        //}
        //FindEntityByName("Floor").GetComponent<MeshRenderer>().mesh = mesh;
    }

    public void StartGameCallback()
    {
        FindEntityByName("StartGui").GetComponent<GuiComponent>().Enabled = false;
    }

    public void CloseGameCallback()
    {
        InternalCalls.CloseGame();
    }

    public bool LastFrameEWasPressed = false;

    public void OnUpdate()
    {
        MovePlayer();
        RotateCamera();
        SpeedControl();

        if (Input.IsKeyDown(KeyCode.Escape))
        {
            FindEntityByName("StartGui").GetComponent<GuiComponent>().Enabled = true;
        }

        if (LastFrameEWasPressed && Input.IsKeyReleased(KeyCode.E))
        {
            FindEntityByName("Box" + CurrentDimension).GetComponent<MeshRenderer>().Enabled = true;//!FindEntityByName("Box" + CurrentDimension).GetComponent<MeshRenderer>().Enabled;
            CurrentDimension = CurrentDimension == 1 ? 2 : 1;
            FindEntityByName("Box" + CurrentDimension).GetComponent<MeshRenderer>().Enabled = false;//!FindEntityByName("Box" + CurrentDimension).GetComponent<MeshRenderer>().Enabled;
        }

        if (Input.IsKeyDown(KeyCode.E))
        {
            LastFrameEWasPressed = true;
        }
        else
            LastFrameEWasPressed = false;

        HandleMagnet();
    }

    public void HandleMagnet()
    {
        Entity target = FindEntityByName("Box" + CurrentDimension);
        Vector3 targetPosition = target.GetComponent<Transform>().Translation;
        Vector3 bodyPosition = this.GetComponent<Transform>().Translation;
        float distance = Vector3.Distance(targetPosition, bodyPosition);
        if (distance > 1.5f && distance < 15.0f)
        {
            if (Input.IsMouseDown(0))
            {
                FindEntityByName("Box1").GetComponent<RigidBody>().AddForce(Vector3.Normalize(Box1Transform.Translation - bodyPosition) * -1.0f * 1000.0f);
                FindEntityByName("Box2").GetComponent<RigidBody>().AddForce(Vector3.Normalize(Box2Transform.Translation - bodyPosition) * 1000.0f);
            }
            else if (Input.IsMouseDown(1))
            {
                FindEntityByName("Box1").GetComponent<RigidBody>().AddForce(Vector3.Normalize(Box1Transform.Translation - bodyPosition) * 1000.0f);
                FindEntityByName("Box2").GetComponent<RigidBody>().AddForce(Vector3.Normalize(Box2Transform.Translation - bodyPosition) * -1.0f * 1000.0f);
            }
        }
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
