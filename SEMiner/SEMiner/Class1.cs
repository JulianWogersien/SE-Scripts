using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game;
using VRageMath;

namespace SpaceEngineersScript
{
    public sealed class Program : MyGridProgram
    {
        // note: this script is not actually finished/working and im not sure ill ever finish it anyways.
        private IMyMotorStator _minerRotor;
        private IMyPistonBase _pistonOut;
        private IMyPistonBase _pistonDown;

        // Configuration
        private const float ROTOR_SPEED = 1.0f;           // RPM
        private const float PISTON_SPEED = 0.1f;          // m/s
        private const float PISTON_STEP = 1.0f;           // meters
        private const float ANGLE_TOLERANCE = 0.1f;       // degrees
        private const float POSITION_TOLERANCE = 0.01f;   // meters

        private enum MiningState
        {
            Initializing,
            Rotating,
            ExtendingOut,
            RetractingOut,
            ExtendingDown,
            Complete
        }
        private MiningState _currentState = MiningState.Initializing;
        private float _startAngle;
        private float _targetOutPosition;
        private int _rotationCount = 0;
        private float _lastPistonDownState = 0f;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            Initialize();
        }

        private void Initialize()
        {
            _minerRotor = GridTerminalSystem.GetBlockWithName("MinerRotor") as IMyMotorStator;
            _pistonOut = GridTerminalSystem.GetBlockWithName("MinerPistonOut") as IMyPistonBase;
            _pistonDown = GridTerminalSystem.GetBlockWithName("MinerPistonDown") as IMyPistonBase;

            if (_minerRotor == null || _pistonOut == null || _pistonDown == null)
            {
                Echo("Error: Could not find all required blocks!");
                Runtime.UpdateFrequency = UpdateFrequency.None;
                return;
            }

            _minerRotor.Enabled = true;
            _pistonOut.Enabled = true;
            _pistonDown.Enabled = true;

            _minerRotor.RotateToAngle(MyRotationDirection.AUTO, 0.0f, 1.0f);

            _startAngle = _minerRotor.Angle;
            _targetOutPosition = 0;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (updateSource == UpdateType.Terminal && argument == "reset")
            {
                _currentState = MiningState.Initializing;
                Initialize();
                return;
            }

            switch (_currentState)
            {
                case MiningState.Initializing:
                    StartRotation();
                    break;

                case MiningState.Rotating:
                    HandleRotation();
                    break;

                case MiningState.ExtendingOut:
                    HandlePistonOut();
                    break;

                case MiningState.RetractingOut:
                    HandlePistonRetract();
                    break;

                case MiningState.ExtendingDown:
                    HandlePistonDown();
                    break;

                case MiningState.Complete:
                    HandleComplete();
                    break;
            }

            DisplayStatus();
        }

        private void StartRotation()
        {
            _minerRotor.TargetVelocityRPM = ROTOR_SPEED;
            _startAngle = _minerRotor.Angle;
            _currentState = MiningState.Rotating;
        }

        private void HandleRotation()
        {
            float angleDifference = Math.Abs(_minerRotor.Angle - _startAngle);
            

            if (_rotationCount > 0)
            {
                _minerRotor.TargetVelocityRPM = 0;
                _rotationCount = 0;

                if (_pistonOut.CurrentPosition >= _pistonOut.MaxLimit - POSITION_TOLERANCE)
                {
                    _currentState = MiningState.RetractingOut;
                    _targetOutPosition = 0;
                }
                else
                {
                    _currentState = MiningState.ExtendingOut;
                    _targetOutPosition = Math.Min(_pistonOut.CurrentPosition + PISTON_STEP, _pistonOut.MaxLimit);
                }
            }
            else if (angleDifference > MathHelper.TwoPi - ANGLE_TOLERANCE)
            {
                _rotationCount++;
            }
        }

        private void HandlePistonOut()
        {
            _pistonOut.Velocity = PISTON_SPEED;

            if (Math.Abs(_pistonOut.CurrentPosition - _targetOutPosition) < POSITION_TOLERANCE)
            {
                _pistonOut.Velocity = 0;
                StartRotation();
            }
        }

        private void HandlePistonRetract()
        {
            _pistonOut.Velocity = -PISTON_SPEED;

            if (_pistonOut.CurrentPosition < POSITION_TOLERANCE)
            {
                _pistonOut.Velocity = 0;
                _currentState = MiningState.ExtendingDown;
            }
        }

        private void HandlePistonDown()
        {
            if (_pistonDown.CurrentPosition >= _pistonDown.MaxLimit - POSITION_TOLERANCE)
            {
                _pistonDown.Velocity = 0;
                _currentState = MiningState.Complete;
                return;
            }

            _pistonDown.Velocity = PISTON_SPEED;

            if (_pistonDown.CurrentPosition >= _lastPistonDownState + PISTON_STEP - POSITION_TOLERANCE)
            {
                _pistonDown.Velocity = 0;
                StartRotation();
            }
        }

        private void HandleComplete()
        {
            _minerRotor.TargetVelocityRPM = 0;
            _pistonOut.Velocity = 0;
            _pistonDown.Velocity = 0;
            Echo("Mining sequence complete!");
        }

        private void DisplayStatus()
        {
            Echo($"State: {_currentState}");
            Echo($"Rotor Angle: {_minerRotor.Angle:F2}");
            Echo($"Piston Out: {_pistonOut.CurrentPosition:F2}m");
            Echo($"Piston Down: {_pistonDown.CurrentPosition:F2}m");
        }

        public void Save() { }
    }
}