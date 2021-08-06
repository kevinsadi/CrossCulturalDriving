/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using UnityEngine.InputSystem;

using System.Collections;

public class VehicleInputControllerNetworked : MonoBehaviour
{

    public Transform SteeringWheel;
    float steeringAngle;
    private VehicleController controller;
    private SteeringWheelInputController steeringInput;

    public bool useKeyBoard;
    float transitionlerp;

    public Transform[] Left;
    public Transform[] Right;
    public Transform[] BrakeLightObjects;
    public Material baseMaterial;

    public Material materialOn;
    public Material materialOff;
    public Material materialBrake;


    public Color BrakeColor;
    public Color On;
    public Color Off;

    public bool LeftActive;
    public bool RightActive;

    private bool breakIsOn = false;


    private bool ActualLightOn;
    private bool LeftIsActuallyOn;
    private bool RightIsActuallyOn;
    public bool LeftIndicatorLog { get { return LeftIsActuallyOn; } }
    public bool RightIndicatorLog { get { return RightIsActuallyOn; } }
    public float indicaterTimer;
    public float interval;
    public int indicaterStage;

    public bool DualButtonDebounceIndicator;
    public bool LeftIndicatorDebounce;
    public bool RightIndicatorDebounce;
    private InputAction rightTurnSignalAction;
    private InputAction leftTurnSignalAction;
    private InputAction hornAction;
    AudioSource HonkSound;

    void Awake()
    {
        controller = GetComponent<VehicleController>();

    }
    private void Start()
    {
        rightTurnSignalAction = new InputAction("Right Turn Signal");
        leftTurnSignalAction = new InputAction("Left Turn Signal");
        hornAction = new InputAction("Horn");
        rightTurnSignalAction.AddBinding("<Joystick>/button5");
        leftTurnSignalAction.AddBinding("<Joystick>/button6");
        hornAction.AddBinding("<Joystick>/trigger");
        hornAction.AddBinding("<Joystick>/button2");
        hornAction.AddBinding("<Joystick>/button3");
        hornAction.AddBinding("<Joystick>/button4");
        hornAction.AddBinding("<Joystick>/button7");
        hornAction.AddBinding("<Joystick>/button8");
        hornAction.AddBinding("<Joystick>/button9");
        hornAction.AddBinding("<Joystick>/button10");
        hornAction.AddBinding("<Joystick>/button11");
        hornAction.AddBinding("<Joystick>/button12");
        hornAction.AddBinding("<Joystick>/button20");
        hornAction.AddBinding("<Joystick>/button21");
        hornAction.AddBinding("<Joystick>/button22");
        hornAction.AddBinding("<Joystick>/button23");
        hornAction.AddBinding("<Joystick>/button24");
        hornAction.AddBinding("<Joystick>/hat/down");
        hornAction.AddBinding("<Joystick>/hat/up");
        hornAction.AddBinding("<Joystick>/hat/left");
        hornAction.AddBinding("<Joystick>/hat/right");
        rightTurnSignalAction.Enable();
        leftTurnSignalAction.Enable();
        hornAction.Enable();

        steeringInput = GetComponent<SteeringWheelInputController>();
        indicaterStage = 0;

        //Generating a new material for on/off;
        materialOn = new Material(baseMaterial);
        materialOn.SetColor("_Color", On);
        materialOff = new Material(baseMaterial);
        materialOff.SetColor("_Color", Off);
        materialBrake = new Material(baseMaterial);
        materialBrake.SetColor("_Color", BrakeColor);


        HonkSound = GetComponent<AudioSource>();

        foreach (Transform t in Left)
        {
            t.GetComponent<MeshRenderer>().material = materialOff;
        }
        foreach (Transform t in Right)
        {
            t.GetComponent<MeshRenderer>().material = materialOff;
        }
        foreach (Transform t in BrakeLightObjects)
        {
            t.GetComponent<MeshRenderer>().material = materialOff;
        }

    }
    void startBlinking(bool left, bool right)
    {
       
        indicaterStage = 1;
        if (left == right == true)
        {
            if (LeftIsActuallyOn != true || RightIsActuallyOn != true)
            {
                LeftIsActuallyOn = true;
                RightIsActuallyOn = true;
            }
            else if (LeftIsActuallyOn == RightIsActuallyOn == true)
            {
                RightIsActuallyOn = false;
                LeftIsActuallyOn = false;
            }
        }
        if (left != right)
        {

            if (LeftIsActuallyOn == RightIsActuallyOn == true) // When we are returning from the hazard lights we make sure that not the inverse thing turns on 
            {
                LeftIsActuallyOn = false;
                RightIsActuallyOn = false;
            }
            if (left)
            {
                if (!LeftIsActuallyOn)
                {
                    LeftIsActuallyOn = true;
                    RightIsActuallyOn = false;
                }
                else
                {
                    LeftIsActuallyOn = false;
                }
            }
            if (right)
            {
                if (!RightIsActuallyOn)
                {
                    LeftIsActuallyOn = false;
                    RightIsActuallyOn = true;
                }
                else
                {

                    RightIsActuallyOn = false;
                }
            }
        }

    }

    void UpdateIndicator()
    {
        if (indicaterStage == 1)
        {
            indicaterStage = 2;
            indicaterTimer = 0;
            ActualLightOn = false;
        }
        else if (indicaterStage == 2 || indicaterStage == 3)
        {
            indicaterTimer += Time.deltaTime;

            if (indicaterTimer > interval)
            {
                indicaterTimer = 0;
                ActualLightOn = !ActualLightOn;
                if (ActualLightOn)
                {
                    CmdUpdateIndicatorLights(LeftIsActuallyOn, RightIsActuallyOn);
                }
                else
                {
                    CmdUpdateIndicatorLights(false, false);
                }
            }
            if (indicaterStage == 2)
            {

                if (Mathf.Abs(steeringInput.GetSteerInput() * -450f) > 90)
                { // steering wheel angle detection to turn of the indicator
                    indicaterStage = 3;
                }
            }
            else if (indicaterStage == 3)
            {
                if (Mathf.Abs(steeringInput.GetSteerInput() * -450f) < 10)
                {
                    indicaterStage = 4;
                }
            }

        }
        else if (indicaterStage == 4)
        {
            indicaterStage = 0;
            ActualLightOn = false;
            LeftIsActuallyOn = false;
            RightIsActuallyOn = false;
            CmdUpdateIndicatorLights(false, false);

        }
    }

  
    public void CmdUpdateIndicatorLights(bool Left, bool Right)
    {
        RpcTurnOnLeft(Left);
        RpcTurnOnRight(Right);

    }
   
    public void RpcTurnOnLeft(bool Leftl_)
    {
        if (Leftl_)
        {
            foreach (Transform t in Left)
            {
                t.GetComponent<MeshRenderer>().material = materialOn;
            }
        }
        else
        {
            foreach (Transform t in Left)
            {
                t.GetComponent<MeshRenderer>().material = materialOff;
            }
        }
    }
    
    public void RpcTurnOnRight(bool Rightl_)
    {
        if (Rightl_)
        {
            foreach (Transform t in Right)
            {
                t.GetComponent<MeshRenderer>().material = materialOn;
            }
        }
        else
        {
            foreach (Transform t in Right)
            {
                t.GetComponent<MeshRenderer>().material = materialOff;
            }
        }

    }

    
    public void CmdStartQuestionairGloablly()
    {
        RpcRunQuestionairNow();
    }

   
    public void RpcRunQuestionairNow()
    {
        FindObjectOfType<SpecificSceneManager>().runQuestionairNow();

    }
    
    public void CmdStartWalking()
    {
        RpcStartWallking();
    }

    
    public void RpcStartWallking()
    {
        foreach (MaleAvatarController a in FindObjectsOfType<MaleAvatarController>())
        {
            a.ChooseAnimation(1);
        }
    }

   
    public void CmdSwitchBrakeLight(bool Active)
    {
        RpcTurnOnBrakeLight(Active);

    }
   
    public void RpcTurnOnBrakeLight(bool Active)
    {
        if (Active)
        {
            foreach (Transform t in BrakeLightObjects)
            {
                t.GetComponent<MeshRenderer>().material = materialBrake;
            }
        }
        else
        {
            foreach (Transform t in BrakeLightObjects)
            {
                t.GetComponent<MeshRenderer>().material = materialOff;
            }
        }
    }


   
    public void CmdStartDriving()
    {
        RpcSetToDrive();
    }
    
    public void RpcSetToDrive()
    {
        //SceneStateManager.Instance.SetDriving();
    }


    


    public void CmdHonkMyCar()
    {
        RpcHonkmyCar();
    }

   
    public void RpcHonkmyCar()
    {
        HonkSound.Play();
    }



    public void RpcSetGPS(GpsController.Direction[] dir)
    {
       // GetComponentInChildren<GpsController>().SetDirection(dir[SceneStateManager.Instance.getParticipantID()]);
    }

    void Update()
    {
       

        //ToDo :Fix both the input (new input system and button layout 
        //ToDo: And the output 
        
            if (Input.GetKeyDown(KeyCode.Return))
            {
                CmdStartDriving();
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                CmdStartQuestionairGloablly();

            }
            if (Input.GetKeyDown(KeyCode.A))
            {
                GetComponentInChildren<GpsController>().SetDirection(GpsController.Direction.Left);
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                GetComponentInChildren<GpsController>().SetDirection(GpsController.Direction.Right);
            }
            if (Input.GetKeyDown(KeyCode.W))
            {
                GetComponentInChildren<GpsController>().SetDirection(GpsController.Direction.Straight);
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
                GetComponentInChildren<GpsController>().SetDirection(GpsController.Direction.Stop);
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                GetComponentInChildren<GpsController>().SetDirection(GpsController.Direction.Hurry);
            }
            if (Input.GetKeyDown(KeyCode.Y))
            {
                CmdStartWalking();
            }



            //if (SceneStateManager.Instance.ActionState == ActionState.DRIVE) // Check that we are drviving
           // {


                transitionlerp = 0;
                if (steeringInput == null || useKeyBoard)
                {
                    controller.steerInput = Input.GetAxis("Horizontal");
                    controller.accellInput = Input.GetAxis("Vertical");
                }
                else
                {
                    controller.steerInput = steeringInput.GetSteerInput();
                    controller.accellInput = steeringInput.GetAccelInput();

                    SteeringWheel.RotateAround(SteeringWheel.position, SteeringWheel.up, steeringAngle - steeringInput.GetSteerInput() * -450f);
                    steeringAngle = steeringInput.GetSteerInput() * -450f;
                }
        //bool TempLeft = Input.GetButton("indicateLeft");
                bool TempLeft = leftTurnSignalAction.ReadValue<float>() != 0;
        //bool TempRight = Input.GetButton("indicateRight");
                bool TempRight = rightTurnSignalAction.ReadValue<float>() != 0;
               // Debug.Log(TempLeft.ToString()+ TempRight.ToString());
                if (TempLeft || TempRight)
                {
                    DualButtonDebounceIndicator = true;
                    if (TempLeft)
                    {
                        LeftIndicatorDebounce = true;
                    }
                    if (TempRight)
                    {
                       RightIndicatorDebounce = true;
                    }
                }
                else if (DualButtonDebounceIndicator && !TempLeft && !TempRight) {
                    startBlinking(LeftIndicatorDebounce, RightIndicatorDebounce);
                    DualButtonDebounceIndicator = false;
                    LeftIndicatorDebounce = false;
                    RightIndicatorDebounce = false;
                   
                }




                if (hornAction.triggered)
                {
                    CmdHonkMyCar();
                }

                UpdateIndicator();

                if (controller.accellInput < 0 && !breakIsOn)
                {
                    breakIsOn = true;
                    CmdSwitchBrakeLight(breakIsOn);
                }
                else if (controller.accellInput >= 0 && breakIsOn)
                {
                    breakIsOn = false;
                    CmdSwitchBrakeLight(breakIsOn);
                }

            }
            

               /* if (transitionlerp < 1)
                {
                    transitionlerp += Time.deltaTime * 0.5f;
                    controller.steerInput = Mathf.Lerp(controller.steerInput, 0, transitionlerp);
                    controller.accellInput = Mathf.Lerp(0, -1, transitionlerp);
                    Debug.Log("SteeringWheel: " + controller.steerInput + "\tAccel: " + controller.accellInput);
                }*/
            
        
    
}
