﻿using Duality;
using Duality.Components.Physics;
using Duality.Input;
using Khronos.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Khronos.Khrono
{
    [RequiredComponent(typeof(RigidBody))]
    public class TimeBody : Component, ICmpInitializable, ICmpUpdatable
    {
        public float RecordTime
        {
            get { return recordTime; }
            set { recordTime = value; }
        }

        private float recordTime = 3600f;

        [DontSerialize]
        private Deque<PointInTime> pointsInTime;

        [DontSerialize]
        private bool isTimeWalking = false;

        [DontSerialize]
        private int currentBufferIndex = 0;
        [DontSerialize]
        private int bufferChangeStep = 0;

        Action _OnComplete;

        private RigidBody body;

        public void OnInit(InitContext context)
        {
            if (context == InitContext.Activate)
            {
                body = GameObj.GetComponent<RigidBody>();
                pointsInTime = new Deque<PointInTime>();
            }
        }

        public void OnShutdown(ShutdownContext context)
        {
        }

        public void UpdateTimeWalk()
        {
            if (pointsInTime.Count > 0)
            {
                var pointInTime = pointsInTime[currentBufferIndex];
                GameObj.Transform.Pos = pointInTime.Position;
                GameObj.Transform.Angle = pointInTime.Rotation;
                body.LinearVelocity = pointInTime.Velocity;
                body.AngularVelocity = pointInTime.AngularVelocity;
                currentBufferIndex += bufferChangeStep;

                //After the currentBufferIndex has been adjusted, it is safe to check if it is at an edge
                if (currentBufferIndex == 0 || currentBufferIndex == pointsInTime.Count)
                {
                    TimeWalkFinished();
                }
            }
            else
            {
                TimeWalkFinished();
            }
        }

        private void TimeWalkFinished()
        {
            isTimeWalking = false;
            currentBufferIndex = -1;
            bufferChangeStep = 0;
            if (_OnComplete != null)
                _OnComplete();
        }

        public void Record()
        {
            if (pointsInTime.Count > MathF.Round(recordTime / Time.TimeMult))
            {
                pointsInTime.RemoveFromFront();
            }

            pointsInTime.AddToBack(new PointInTime(GameObj.Transform.Pos, GameObj.Transform.Angle, body.LinearVelocity, body.AngularVelocity));
        }

        public void StartRewind(Action onComplete)
        {
            isTimeWalking = true;
            currentBufferIndex = pointsInTime.Count - 1;
            bufferChangeStep = -1;
            _OnComplete = onComplete;
        }

        public void StartReplay(Action onComplete)
        {
            isTimeWalking = true;
            currentBufferIndex = 0;
            bufferChangeStep = 1;
            _OnComplete = onComplete;
        }

        public void OnUpdate()
        {
            if (isTimeWalking)
                UpdateTimeWalk();
            else
                Record();
        }
    }
}