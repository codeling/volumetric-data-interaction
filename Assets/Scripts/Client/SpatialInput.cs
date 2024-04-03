﻿#nullable enable

using System;
using UnityEngine;

namespace Client
{
    /// <summary>
    /// Detect Shake: https://resocoder.com/2018/07/20/shake-detecion-for-mobile-devices-in-unity-android-ios/
    /// To use the class Gyroscope, the device needs to have a gyroscope
    /// </summary>
    public class SpatialInput : MonoBehaviour
    {
        public event Action<int>? Shook;
        public event Action<bool>? Tilted;
        
        private const float MinInputInterval = 0.2f; // 0.2sec - to avoid detecting multiple shakes per shake
        private int _shakeCounter;

        private InputTracker _shakeTracker = null!;
        private InputTracker _tiltTracker = null!;

        private Gyroscope _deviceGyroscope = null!;

        private void Start()
        {
            _shakeTracker = new InputTracker
            {
                Threshold = 5f
            };

            _tiltTracker = new InputTracker
            {
                Threshold = 1.3f,
                TimeSinceLast = Time.unscaledTime
            };
            
            _deviceGyroscope = Input.gyro;
            _deviceGyroscope.enabled = true;
        }

        private void Update()
        {
            if (_shakeCounter > 0 && Time.unscaledTime > _shakeTracker.TimeSinceFirst + MinInputInterval * 5)
            {
                HandleShakeInput();
                _shakeCounter = 0;
            }        

            CheckShakeInput();
            CheckTiltInput();
        }

        private void CheckShakeInput()
        {
            if (Input.acceleration.sqrMagnitude >= _shakeTracker.Threshold
                && Time.unscaledTime >= _shakeTracker.TimeSinceLast + MinInputInterval)
            {
                _shakeTracker.TimeSinceLast = Time.unscaledTime;

                if (_shakeCounter == 0)
                {
                    _shakeTracker.TimeSinceFirst = _shakeTracker.TimeSinceLast;
                }

                _shakeCounter++;
            }
        }

        private void HandleShakeInput()
        {
            _shakeTracker.TimeSinceLast = Time.unscaledTime;
            Shook?.Invoke(_shakeCounter);
        }

        /// <summary>
        /// https://docs.unity3d.com/2019.4/Documentation/ScriptReference/Input-gyro.html
        /// https://answers.unity.com/questions/1284652/inputgyroattitude-returns-zero-values-when-tested.html
        /// attitude does not work on all tablets / samsung galaxy s6 tab
        /// </summary>
        private void CheckTiltInput()
        {
            if (Time.unscaledTime >= _tiltTracker.TimeSinceLast + MinInputInterval * 5)
            {
                var horizontalTilt = _deviceGyroscope.rotationRateUnbiased.y; 

                if (Math.Abs(horizontalTilt) < _tiltTracker.Threshold)
                {
                    return;
                }

                _tiltTracker.TimeSinceLast = Time.unscaledTime;

                Tilted?.Invoke(horizontalTilt > 0);
            }
        }
    }
}
