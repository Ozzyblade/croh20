/** A simple camera controller allowing the user to move around the scene during 
 * runtime. Controls are WASD and mouse input */
using UnityEngine;

public class CG_CameraController : MonoBehaviour {

    [SerializeField] private float camSpeed = 2.0f;
    [SerializeField] private float boostSpeed = 10.0f;
    [SerializeField] private float maxSpeed = 20.0f;

    [SerializeField] private float mouseSensitivity = 0.25f;
    private Vector3 lastMousePosition = new Vector3(255, 255, 255);
    private float totalRun = 1.0f;

    [SerializeField] private bool bHideCursor = false;

    private void Start()
    {
        Cursor.visible = !bHideCursor;
    }

    // Update is called once per frame
    void Update () {
        lastMousePosition = Input.mousePosition - lastMousePosition;
        lastMousePosition = new Vector3(-lastMousePosition.y * mouseSensitivity, lastMousePosition.x * mouseSensitivity, 0);
        lastMousePosition = new Vector3(transform.eulerAngles.x + lastMousePosition.x, transform.eulerAngles.y + lastMousePosition.y, 0);

        transform.eulerAngles = lastMousePosition;
        lastMousePosition = Input.mousePosition;

        float f = 0.0f;
        Vector3 p = GetBaseInput();

        if (Input.GetKey(KeyCode.LeftShift))
        {
            totalRun += Time.deltaTime;

            p = p * totalRun * boostSpeed;
            p.x = Mathf.Clamp(p.x, -maxSpeed, maxSpeed);
            p.y = Mathf.Clamp(p.y, -maxSpeed, maxSpeed);
            p.z = Mathf.Clamp(p.z, -maxSpeed, maxSpeed);
        }
        else
        {
            totalRun = Mathf.Clamp(totalRun * 0.5f, 1.0f, 100.0f);
            p = p * camSpeed;
        }

        p = p * Time.deltaTime; ;

        Vector3 newPos = transform.position;

        if (Input.GetKey(KeyCode.Space))
        {
            transform.Translate(p);
            newPos.x = transform.position.x;
            newPos.z = transform.position.z;
            transform.position = newPos;
        }
        else
        {
            transform.Translate(p);
        }
    }

    private Vector3 GetBaseInput()
    {
        Vector3 p_Vel = new Vector3();

        if (Input.GetKey(KeyCode.W))
            p_Vel += new Vector3(0, 0, 1);

        if (Input.GetKey(KeyCode.S))
            p_Vel += new Vector3(0, 0, -1);

        if (Input.GetKey(KeyCode.A))
            p_Vel += new Vector3(-1, 0, 0);

        if (Input.GetKey(KeyCode.D))
            p_Vel += new Vector3(1, 0, 0);

        return p_Vel;
    }
}
