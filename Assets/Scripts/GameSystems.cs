using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class GameSystems : MonoBehaviour
{
    [SerializeField] private Button[] _colorButtons;
    [SerializeField] private GameObject _markerPrefab;
    [SerializeField] private GameObject _scannerPrefab;
    [SerializeField] private GameObject[] _particlePrefabs;
    [SerializeField] private Gradient _scannerGradient;
    [SerializeField] private TextMeshProUGUI _scannerButtonText;

    private ARRaycastManager _ARRaycastManager;
    private GameObject _curMarker;
    private List<ARRaycastHit> _hits;
    private List<GameObject> _spawnedParticles;

    private bool _scannerOn = false;
    private int _particleID = 0;

    private void Awake()
    {
        _ARRaycastManager = FindObjectOfType<ARRaycastManager>();
        _hits = new List<ARRaycastHit>();
        _spawnedParticles = new List<GameObject>();

        _colorButtons[_particleID].interactable = false;
    }

    private void Update()
    {
        SetMarker();
        TrySpawnParticle();
    }

    private void SetMarker()
    {
        _ARRaycastManager.Raycast(new Vector2(Screen.width / 2, Screen.height / 2), _hits, TrackableType.Planes);

        if (_hits.Count > 0)
        {
            if (_curMarker)
            {
                _curMarker.transform.position = _hits[0].pose.position;
                _curMarker.transform.rotation = _hits[0].pose.rotation;
            }
            else
                _curMarker = Instantiate(_markerPrefab, _hits[0].pose.position, _hits[0].pose.rotation);
        }
        else if (_curMarker)
            Destroy(_curMarker);
    }

    private void TrySpawnParticle()
    {
        if (Input.touchCount == 0)
            return;

        Touch _touch = Input.GetTouch(0);

        if (_touch.phase != TouchPhase.Ended || CheckOverUI(_touch))
            return;

        _ARRaycastManager.Raycast(_touch.position, _hits, TrackableType.Planes);

        if (_hits.Count == 0)
            return;

        _spawnedParticles.Add(Instantiate(_particlePrefabs[_particleID], _hits[0].pose.position, _hits[0].pose.rotation));
    }

    private void ScanEnvironment()
    {
        int x = Screen.width / 200;
        int y = Screen.height / 200;

        for (int i = 0; i < x; i++)
        {
            for (int j = 0; j < y; j++)
            {
                Vector2 _pos = new Vector2(i * 200 + 100, j * 200 + 100);

                _ARRaycastManager.Raycast(_pos, _hits, TrackableType.Planes);

                if (_hits.Count == 0)
                    break;

                ParticleSystem _curScanner = Instantiate(_scannerPrefab, _hits[0].pose.position, Quaternion.identity).GetComponent<ParticleSystem>();

                float _colorByDistance = Mathf.Clamp(_hits[0].distance, 0, 3) / 3;
                ParticleSystem.MainModule _curMain = _curScanner.main;
                _curMain.startColor = _scannerGradient.Evaluate(_colorByDistance);
            }
        }
    }

    private bool CheckOverUI(Touch _checkedTouch)
    {
        PointerEventData _pointerEventData = new PointerEventData(EventSystem.current);
        _pointerEventData.position = _checkedTouch.position;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(_pointerEventData, results);

        foreach (var item in results)
        {
            if (item.gameObject.GetComponent<CanvasRenderer>())
                return true;
        }

        return false;
    }

    public void ChangeColor(int _setID)
    {
        _colorButtons[_particleID].interactable = true;

        _particleID = _setID;

        _colorButtons[_particleID].interactable = false;
    }

    public void ClearParticles()
    {
        foreach (var item in _spawnedParticles)
            Destroy(item);

        _spawnedParticles.Clear();
    }

    public void SwitchScanner()
    {
        _scannerOn = !_scannerOn;

        if (_scannerOn)
        {
            StartCoroutine(ScanDelay());
            _scannerButtonText.text = "Scanner: on";
        }
        else
        {
            StopAllCoroutines();
            _scannerButtonText.text = "Scanner: off";
        }
    }

    public void ResetScene()
    {
        FindObjectOfType<ARSession>().Reset();
    }

    private IEnumerator ScanDelay()
    {
        yield return new WaitForSeconds(0.5f);

        ScanEnvironment();
        StartCoroutine(ScanDelay());
    }
}