using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private CinemachineCamera[] menuCameras;
    [SerializeField] private float cameraSwitchInterval = 5f;

    public event Action OnStartRequested;

    private Coroutine   _cycleRoutine;
    private int         _currentIndex;
    private IDisposable _buttonListener;

    public void Activate()
    {
        _currentIndex = 0;
        SetActiveCamera(0);
        _cycleRoutine   = StartCoroutine(CycleRoutine());
        _buttonListener = InputSystem.onAnyButtonPress.Call(HandleAnyButton);
    }

    public void Deactivate()
    {
        if (_cycleRoutine != null) StopCoroutine(_cycleRoutine);
        _buttonListener?.Dispose();
        _buttonListener = null;
        foreach (var cam in menuCameras)
            if (cam != null) cam.gameObject.SetActive(false);
    }

    private IEnumerator CycleRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(cameraSwitchInterval);
            _currentIndex = (_currentIndex + 1) % menuCameras.Length;
            SetActiveCamera(_currentIndex);
        }
    }

    private void SetActiveCamera(int index)
    {
        for (int i = 0; i < menuCameras.Length; i++)
            if (menuCameras[i] != null)
                menuCameras[i].gameObject.SetActive(i == index);
    }

    private void HandleAnyButton(InputControl ctrl)
    {
        _buttonListener?.Dispose();
        _buttonListener = null;
        OnStartRequested?.Invoke();
    }
}
