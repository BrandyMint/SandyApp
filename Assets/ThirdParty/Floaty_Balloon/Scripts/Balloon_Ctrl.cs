using UnityEngine;
using System.Collections;

public class Balloon_Ctrl : MonoBehaviour {

    public float sensitivity = 250f;
    public float upForce = 100f;
    public bool noRightClick;
    public readonly float dwnForce = 0;//use gravity instead as constant downward force
    private float xForce;
    private Vector3 defaultPos;
    private Vector3 balloonPos;
    private ConstantForce constForce;
    private Vector3 worldPoint;
    private Vector3 mousePos;
    private Vector3 newPos;

	void Start () {
        defaultPos = Input.acceleration;
        constForce = gameObject.GetComponent<ConstantForce>();
    }

    void FixedUpdate () {
     if (Input.GetMouseButton(0))//recognized as left mouse & one finger touch (balloon force up)
     {
         if (Input.acceleration != defaultPos)//mobile accel will almost never be same as defaultPos on mobile devices
         {   
             xForce = Input.acceleration.x * sensitivity;
         }
         else if (Input.GetMouseButton(1) || noRightClick)//pc only
         {
             GetXforce();
         }
         constForce.force = new Vector3(xForce, upForce, 0);
     }
     else//if the balloon is floating down
     {
         if(Input.acceleration != defaultPos)
         {
             xForce = Input.acceleration.x * sensitivity;
         }
         else
         {
             GetXforce();
         }
         constForce.force = new Vector3(xForce, dwnForce, 0);
     }
    }

    void GetXforce()
    {
        balloonPos = gameObject.transform.position;
        mousePos = Input.mousePosition;
        mousePos.z = transform.position.z - Camera.main.transform.position.z;//distance from camera
        worldPoint = Camera.main.ScreenToWorldPoint(mousePos);
        newPos = worldPoint - balloonPos;
        xForce = (newPos.x * (sensitivity / 10));
    }
}
