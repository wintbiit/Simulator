//Laser Controller
//This code can be used for private or commercial projects but cannot be sold or redistributed without written permission.
//Copyright Nik W. Kraus / Dark Cube Entertainment LLC. 

using UnityEngine;
using System.Collections;
using System.Linq;
using Script.JudgeSystem.Robot;

//#pragma strict
[RequireComponent(typeof(LineRenderer))]

public class BasicLaser_C : MonoBehaviour
{

    [Tooltip(("Current camera"))] public Camera camera;

    [Tooltip("Laser Start Point transform.")]
    public Transform StartPoint;

    [Tooltip("Local direction of the Laser from Start Point.")]
    public string LaserDirection = "Z";

    [Tooltip("Enable 2D mode, If off 3D mode is used.")]
    public bool Use2D = false;

    [Tooltip("Physics Raycast Masking.")]
    public LayerMask LaserMask = 1;

    public bool LaserOn = true;

    [Tooltip("Enable UVPan to simulate dust.")]
    public bool UseUVPan = true;

    public float EndFlareOffset = 0.05f;

    public LensFlare SourceFlare;
	public LensFlare EndFlare;

	public bool AddSourceLight = true;
    public bool AddEndLight = true;

    public Color LaserColor = new Color(1f,1f,1f,.5f);

    [Tooltip("Width of Laser.")]
    public float StartWidth = 0.1f;
    public float EndWidth = 0.1f;

    [Tooltip("Maximum distance of Laser.")]
    public float LaserDist = 20.0f;

    [Tooltip("Texture scroll speed X and Y (Dust Effect).")]
    public float TexScrollX = -0.01f;
    public float TexScrollY = 0.005f;

    [Tooltip("Texture scale X and Y.")]
    public Vector2 UVTexScale = new Vector2(8f, 0.0f);

    private int SectionDetail = 2;
    private LineRenderer lineRenderer;
    private Ray ray;
    private Vector3 EndPos;
	private RaycastHit hit;
	private RaycastHit2D hit2D;
	private GameObject SourceLight;
	private GameObject EndLight;
	private float ViewAngle;
	private Vector3 LaserDir;	    


    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer.GetComponent<Renderer>().sharedMaterial == null){
            lineRenderer.GetComponent<Renderer>().material = new Material(Shader.Find("LaserAdditive"));
        }

        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;

        lineRenderer.positionCount = SectionDetail;
        lineRenderer = GetComponent<LineRenderer>();

        if (!StartPoint) {
            Debug.Log("If a StartPoint is not assigned, this object transform will be used.");
            StartPoint = gameObject.transform;
        }

        // Make a lights
        if (AddSourceLight)
        {
            StartPoint.gameObject.AddComponent<Light>();
            StartPoint.GetComponent<Light>().intensity = 1.5f;
            StartPoint.GetComponent<Light>().range = .5f;
        }

        if (AddEndLight)
        {
            if (EndFlare)
            {
                EndFlare.gameObject.AddComponent<Light>();
                EndFlare.GetComponent<Light>().intensity = 1.5f;
                EndFlare.GetComponent<Light>().range = .5f;
            }
            else { Debug.Log("To use End Light, please assign an End Flare"); }
        }


        if (LaserDirection == "x" || LaserDirection == "y" || LaserDirection == "z" || LaserDirection == "X" || LaserDirection == "Y" || LaserDirection == "Z")
            {
            }
        else {
            Debug.Log("Laser Direction can only be X, Y or Z");
        }


    }//end start


    /////////////////////////////////////
    void Update()
    {
        if (LaserDirection == "x" || LaserDirection == "X")
        {
            LaserDirection = "X";
            LaserDir = StartPoint.right;
        }
        else if (LaserDirection == "y" || LaserDirection == "Y")
        {
            LaserDirection = "Y";
            LaserDir = StartPoint.up;
        }
        else if (LaserDirection == "z" || LaserDirection == "Z")
        {
            LaserDirection = "Z";
            LaserDir = StartPoint.forward;
        }
        else {
            LaserDir = StartPoint.forward;
        }

        float CamDistSource = Vector3.Distance(StartPoint.position, camera.transform.position);
        float CamDistEnd = Vector3.Distance(EndPos, camera.transform.position);
        ViewAngle = Vector3.Angle(LaserDir, camera.transform.forward);

        if (LaserOn)
        {
            lineRenderer.enabled = true;
            lineRenderer.startWidth = StartWidth;
            lineRenderer.endWidth = EndWidth;
            lineRenderer.material.color = LaserColor;

            //Flare Control
            if (SourceFlare)
            {
                SourceFlare.color = LaserColor;
                SourceFlare.transform.position = StartPoint.position;

                if (ViewAngle > 155 && CamDistSource < 20 && CamDistSource > 0)
                {
                    SourceFlare.brightness = Mathf.Lerp(SourceFlare.brightness, 20.0f, .001f);
                }
                else {
                    SourceFlare.brightness = Mathf.Lerp(SourceFlare.brightness, 0.1f, .05f);
                }
            }

            if (EndFlare)
            {
                EndFlare.color = LaserColor;

                if (CamDistEnd > 20)
                {
                    EndFlare.brightness = Mathf.Lerp(EndFlare.brightness, 0.0f, .1f);
                }
                else {
                    EndFlare.brightness = Mathf.Lerp(EndFlare.brightness, 5.0f, .1f);
                }
            }// end flare        

            //Light Control
            if (AddSourceLight)
            {
                StartPoint.GetComponent<Light>().color = LaserColor;
                StartPoint.GetComponent<Light>().enabled = true;
            }

            if (AddEndLight)
            {
                if (EndFlare)
                {
                    EndFlare.GetComponent<Light>().color = LaserColor;
                }
            }


            /////////////////////Ray Hit
            if (Use2D)
              {
                hit2D = Physics2D.Raycast(StartPoint.position, LaserDir, LaserDist, LaserMask);

                Ray2D ray2 = new Ray2D(StartPoint.position, LaserDir);
                //float dist2D = Vector3.Distance(StartPoint.position, hit2D.point);
                if (hit2D)
                {
                    EndPos = hit2D.point;

                    if (EndFlare)
                    {
                        EndFlare.enabled = true;

                        if (AddEndLight)
                        {
                            if (EndFlare)
                            {
                                EndFlare.GetComponent<Light>().enabled = true;
                            }
                        }

                        if (EndFlareOffset > 0)
                            EndFlare.transform.position = hit2D.point + hit2D.normal * EndFlareOffset;
                        else
                            EndFlare.transform.position = EndPos;
                    }
                }
                else {
                    if (EndFlare)
                        EndFlare.enabled = false;

                    if (AddEndLight)
                    {
                        if (EndFlare)
                        {
                            EndFlare.GetComponent<Light>().enabled = false;
                        }
                    }

                    EndPos = ray2.GetPoint(LaserDist);
                }
            }
            ///Else 3D Ray
            else {
                ray = new Ray(StartPoint.position, LaserDir);
                //Vector3 NewRay = Vector3(ray.GetPoint);
                if (Physics.Raycast(ray, out hit, LaserDist, LaserMask))
                {
                    EndPos = hit.point;

                    if (EndFlare)
                    {
                        EndFlare.enabled = true;

                        if (AddEndLight)
                        {
                            if (EndFlare)
                            {
                                EndFlare.GetComponent<Light>().enabled = true;
                            }
                        }

                        if (EndFlareOffset > 0)
                            EndFlare.transform.position = hit.point + hit.normal * EndFlareOffset;
                        else
                            EndFlare.transform.position = EndPos;
                    }
                }
                else {
                    if (EndFlare)
                        EndFlare.enabled = false;

                    if (AddEndLight)
                    {
                        if (EndFlare)
                        {
                            EndFlare.GetComponent<Light>().enabled = false;
                        }
                    }

                    EndPos = ray.GetPoint(LaserDist);
                }
            }//end Ray       


            //Debug.DrawLine (StartPoint.position, EndPos, Color.red);

            //Find Distance
            var dist = Vector3.Distance(StartPoint.position, EndPos);

            //Line Render Positions
            lineRenderer.SetPosition(0, StartPoint.position);
            lineRenderer.SetPosition(1, EndPos);

            //Texture Scroller
            if (UseUVPan)
            {
                //lineRenderer.material.SetTextureScale("_Mask", Vector2(dist/4, .1));
                lineRenderer.material.SetTextureScale("_Mask", new Vector2(dist / UVTexScale.x, UVTexScale.y));
                lineRenderer.material.SetTextureOffset("_Mask", new Vector2(TexScrollX * Time.time, TexScrollY * Time.time));
            }

        }
        else {
            lineRenderer.enabled = false;

            if (SourceFlare)
                SourceFlare.enabled = false;

            if (EndFlare)
                EndFlare.enabled = false;

            if (AddSourceLight)
                StartPoint.GetComponent<Light>().enabled = false;

            if (AddEndLight){
                if (EndFlare){
                    EndFlare.GetComponent<Light>().enabled = false;
                }
            }
        }//end Laser On   

    }//end Update

    private Transform GizmoStartPoint;

    /////Icon
    void OnDrawGizmos()
    {
        Gizmos.DrawIcon(transform.position, "LaserIcon.psd", true);
    }


    void OnDrawGizmosSelected(){
        if (!StartPoint){
            GizmoStartPoint = gameObject.transform;
        }
        else {
            GizmoStartPoint = StartPoint;
        }

        if (LaserDirection == "x" || LaserDirection == "X")
        {
            LaserDirection = "X";
            LaserDir = GizmoStartPoint.right;
        }
        else if (LaserDirection == "y" || LaserDirection == "Y")
        {
            LaserDirection = "Y";
            LaserDir = GizmoStartPoint.up;
        }
        else if (LaserDirection == "z" || LaserDirection == "Z")
        {
            LaserDirection = "Z";
            LaserDir = GizmoStartPoint.forward;
        }
        else {
            LaserDir = GizmoStartPoint.forward;
        }

        /////////////
        ray = new Ray(GizmoStartPoint.position, LaserDir);
        EndPos = ray.GetPoint(LaserDist);
        Debug.DrawLine(GizmoStartPoint.position, EndPos, LaserColor);
    }//End Selected

}