using UnityEngine;

public class CameraOrbit : MonoBehaviour
{
    public Transform target;
    public float distance = 6f;
    public float speed = 20f;

    [Header("Cinematic views")]
    public Transform[] viewPoints;
    public float viewLerpSpeed = 3f;

    int _currentView = -1;
    bool _orbitMode = true;

    void LateUpdate()
    {
        if (target == null)
            return;

        if (_orbitMode || viewPoints == null || viewPoints.Length == 0 || _currentView < 0)
        {
            transform.RotateAround(target.position, Vector3.up, speed * Time.deltaTime);
            var desiredPos = (transform.position - target.position).normalized * distance + target.position;
            transform.position = desiredPos;
            transform.LookAt(target);
        }
        else
        {
            var vp = viewPoints[_currentView];
            if (vp == null)
                return;

            transform.position = Vector3.Lerp(transform.position, vp.position, viewLerpSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Lerp(transform.rotation, vp.rotation, viewLerpSpeed * Time.deltaTime);
        }
    }

    public void NextView()
    {
        if (viewPoints == null || viewPoints.Length == 0)
            return;

        _orbitMode = false;
        _currentView++;

        if (_currentView >= viewPoints.Length)
            _currentView = 0;
    }

    public void PreviousView()
    {
        if (viewPoints == null || viewPoints.Length == 0)
            return;

        _orbitMode = false;
        _currentView--;

        if (_currentView < 0)
            _currentView = viewPoints.Length - 1;
    }

    public void EnableOrbitMode()
    {
        _orbitMode = true;
    }
}