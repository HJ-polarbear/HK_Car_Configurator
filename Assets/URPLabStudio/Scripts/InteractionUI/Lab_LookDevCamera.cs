namespace URPLabStudio
{
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class Lab_LookDevCamera : MonoBehaviour {

	public enum RotationAxis {
		XY = 0,
		X,
		Y
	};

	private float 	  m_ScrollDelta;
	private float     m_OrbitX;
	private float	  m_OrbitY;
	private float 	  m_ScrollX;
	private float	  m_ScrollY;

	private float   m_RightClickMousePosY = 1;
	private Vector3 m_RightClickCamPos    = Vector3.one;
	private bool	m_RightClickPressed   = false;

	private Camera    m_LookDevCamera;
	private Transform m_LookDevCameraPivot;

	private const float ORBIT_FALLOFF = 0.25f;
	private const float ZOOM_FALLOFF  = 0.15f;
	private const float PAN_FALLOFF	  = 0.10f;

	public RotationAxis m_RotationAxis;

	[Range(0.0f, 1000.0f)]
	public float m_RotationScale;

	[Range(0.0f, 100.0f)]
	public float m_ZoomScale;

	[Range(0.0f, 100.0f)]
	public float m_PanScale;

	void Awake() {
		m_ScrollDelta = m_OrbitX = m_OrbitY = m_ScrollX = m_ScrollY = 0;
		m_LookDevCamera		 = GetComponentInChildren<Camera>();
		m_LookDevCameraPivot = m_LookDevCamera.transform.parent;
	}

	void Update() {
		OrbitCamera();
		ZoomCamera();
		PanCamera();
	}

	private void OrbitCamera() {
		if(Input.GetMouseButton(0)) {
			m_OrbitY = m_RotationScale * Input.GetAxis("Mouse X") * Time.deltaTime;
			m_OrbitX = m_RotationScale * Input.GetAxis("Mouse Y") * Time.deltaTime;
		} else {
			m_OrbitX = 0;
			m_OrbitY = 0;
		}
		
		if(m_RotationAxis == RotationAxis.XY || m_RotationAxis == RotationAxis.Y) m_LookDevCameraPivot.rotation *= Quaternion.AngleAxis(m_OrbitY, Vector3.up); 
		if(m_RotationAxis == RotationAxis.XY || m_RotationAxis == RotationAxis.X) m_LookDevCameraPivot.rotation *= Quaternion.AngleAxis(m_OrbitX, Vector3.right);
	}

	private void ZoomCamera() {

		if(Input.GetMouseButtonDown(1)) {
			m_RightClickMousePosY = Input.mousePosition.y;
			m_RightClickCamPos    = m_LookDevCamera.transform.position;
			m_RightClickPressed   = true;
		} 
		else if (Input.GetMouseButtonUp(1)) {
			m_RightClickPressed = false;
		}

		if(m_RightClickPressed) {
			float yDist = m_RightClickMousePosY - Input.mousePosition.y;
			m_ScrollDelta = (1f / Screen.height) * Mathf.Abs(yDist) * Mathf.Sign(yDist);
			m_LookDevCamera.transform.position = m_RightClickCamPos + m_LookDevCamera.transform.forward * m_ZoomScale * m_ScrollDelta;
		}
	}

	private void PanCamera() {
		if(Input.GetMouseButton(2)) {
			m_ScrollX = m_PanScale * Input.GetAxis("Mouse X") * Time.deltaTime;
			m_ScrollY = m_PanScale * Input.GetAxis("Mouse Y") * Time.deltaTime;
		} else {
			m_ScrollX = 0;
			m_ScrollY = 0;
		}
		
		m_LookDevCamera.transform.position += -m_ScrollX * m_LookDevCamera.cameraToWorldMatrix.MultiplyVector(Vector3.right);
		m_LookDevCamera.transform.position += -m_ScrollY * m_LookDevCamera.cameraToWorldMatrix.MultiplyVector(Vector3.up);
	}

}
}
