////Cathy Mengying Fang 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Examples
{
    public class PostProcesserRetargeting : PostProcessProvider
    {

        [Header("Projection")]
        public Transform headTransform;
        public GameObject pointPrefabS;
        public GameObject pointS;
        public GameObject pointPrefabH;
        public GameObject pointH;

        public float maxWarp; //demo specific; unit [meter]
        public float warpRangeMin;
        public float warpRangeMax;
        private float retargeted_offset;
        
        public enum Demo { Uzi, Phone, Keypad, Button, Soft, Hard};
        public Demo demo;

        public SceneSwitcher sceneSwitcher;
        public bool isOffset;
        public bool isDynamic;

        public enum WarpHand { Left, Right }
        public WarpHand warpedHand = WarpHand.Left;
        //public GameObject attachObject;

        private bool CheckHand(Hand hand)
        {
            return ((warpedHand == WarpHand.Left && hand.IsLeft) || (warpedHand == WarpHand.Right && hand.IsRight));

        }
        
        public override void ProcessFrame(ref Frame inputFrame)
        {
            passthroughOnly = false;
            // Calculate the position of the head and the basis to calculate shoulder position.
            if (headTransform == null) { headTransform = Camera.main.transform; }
            Vector3 headPos = headTransform.position;
            pointPrefabS = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            pointPrefabH = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            isOffset = sceneSwitcher.offset;
            isDynamic = sceneSwitcher.dynamic;

            foreach (var hand in inputFrame.Hands)
            {
                if (CheckHand(hand) && isOffset) //check which hand is being retargeted
                {

                    //Warp Origin
                    // 1) 왼손의 유도할 지점에 대한 좌표 정보 업데이트 및 표시
                    Vector3 warpOrigin = hand.PalmPosition.ToVector3();
                    Vector3 fingerSoft = (hand.Fingers[(int)Finger.FingerType.TYPE_THUMB].Bone(Bone.BoneType.TYPE_METACARPAL).PrevJoint.ToVector3() 
                        + hand.Fingers[(int)Finger.FingerType.TYPE_INDEX].Bone(Bone.BoneType.TYPE_METACARPAL).PrevJoint.ToVector3()) / 2f;
                    Vector3 fingerHard = hand.Fingers[(int)Finger.FingerType.TYPE_THUMB].Bone(Bone.BoneType.TYPE_METACARPAL).PrevJoint.ToVector3();
                    if (hand.IsLeft)
                    {
                        if (pointS != null)
                            Destroy(pointS);
                        //pointS = Instantiate(pointPrefabS, fingerSoft, Quaternion.identity);
  
                        // 점의 크기와 색상 설정
                        pointS.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
                        pointS.GetComponent<Renderer>().material.color = Color.red;

                        if (pointH != null)
                            Destroy(pointH);
                        //pointH = Instantiate(pointPrefabH, fingerHard, Quaternion.identity);

                        // 점의 크기와 색상 설정
                        pointH.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
                        pointH.GetComponent<Renderer>().material.color = Color.blue;
                    }
                    Debug.Log(hand.IsRight);
                    Debug.Log(hand.IsLeft);
                    //Vector3 handSoft = (hand.Finger.TYPE_THUMB.PalmPosition.ToVector3() + hand.Finger.TYPE_INDEX.PalmPosition.ToVector3()) / 2;
                    // 이쪽 좌표로 유도해야함

                    //Warp Direction
                    Vector3 palmPosition = hand.PalmPosition.ToVector3();
                    Vector3 palmZ = hand.PalmNormal.ToVector3();     // 손바닥의 방향 벡터             
                    Vector3 palmX = hand.PalmNormal.Cross(hand.Direction).Normalized.ToVector3();

                    //float dist = Vector3.Distance(handHard, warpOrigin);
                    // 구간을 나눠서(거리에 따라) 손을 순간이동 시켜야 한다. 거리에 따라서 가중치를 다르게 적용

                    // Warp Input
                    float dist = Mathf.Abs(headPos.x - warpOrigin.x);

                    float distZ = Mathf.Abs(headPos.z - warpOrigin.z);

                    if (demo.Equals(Demo.Phone)) // 휴대폰
                    {
                        // 2)오른손의 z축 거리값에 대한 정도 수정 
                        maxWarp = 0.02f;
                        float warpInput = dist;
                        warpRangeMin = 0.05f;
                        warpRangeMax = 0.08f;
                        retargeted_offset = TransferFuncRising(warpInput, maxWarp, warpRangeMin, warpRangeMax);

                        // 3)오른손의 xy평면 값에 대한 변화량 수정 
                        Vector3 xyOffset = new Vector3(retargeted_offset * palmX.x, retargeted_offset * palmX.y, 0f);
                        xyOffset = Vector3.ClampMagnitude(xyOffset, maxWarp); // x, y 변화량 스케일링
                        warpOrigin += xyOffset;

                        // z축 변화량 설정
                        float zOffset = -retargeted_offset * palmZ.z;
                        warpOrigin += new Vector3(0f, 0f, zOffset);
                    }

                    else if (demo.Equals(Demo.Hard)) // Target type = hard
                    {
                        maxWarp = 0.02f;
                        float warpInput = dist;
                        warpRangeMin = 0.05f;
                        warpRangeMax = 0.08f;
                        retargeted_offset = TransferFuncRising(warpInput, maxWarp, warpRangeMin, warpRangeMax);

                        warpOrigin -= retargeted_offset * palmZ;
                        //hand.SetTransform(warpOrigin, hand.Rotation.ToQuaternion());
                    }

                    else if (demo.Equals(Demo.Uzi)) // 총
                    {
                        maxWarp = 0.07f;
                        float warpInput = dist;
                        warpRangeMin = 0.1f;
                        warpRangeMax = 0.17f;
                        retargeted_offset = TransferFuncRising(warpInput, maxWarp, warpRangeMin, warpRangeMax);
                       
                        warpOrigin += palmX * retargeted_offset;
                    }


                    else if (demo.Equals(Demo.Keypad)) // 키패드
                    {
                        maxWarp = 0.2f;
                        float warpInput = distZ;
                        warpRangeMin = 0.2f;
                        warpRangeMax = 0.4f;
                        retargeted_offset = TransferFuncFalling(warpInput, maxWarp, warpRangeMin, warpRangeMax);
                      
                        warpOrigin -= retargeted_offset * transform.right;
                    }


                    else if (demo.Equals(Demo.Button)) // 버튼
                    {
                        maxWarp = 0.2f;
                        float warpInput = distZ;
                        warpRangeMin = 0.2f;
                        warpRangeMax = 0.4f;
                        retargeted_offset = TransferFuncFalling(warpInput, maxWarp, warpRangeMin, warpRangeMax);

                        warpOrigin -= retargeted_offset * transform.right;
                    }

                    hand.SetTransform(warpOrigin, hand.Rotation.ToQuaternion());

                }


           
            }
        }

        private float TransferFuncRising(float warpInput, float maxWarp, float warpRangeMin, float warpRangeMax)
        {
            float offset;
            if (warpInput <= warpRangeMin) { offset = maxWarp; }
            else if (warpInput >= warpRangeMax) { offset = 0; }
            else { offset = warpRangeMax - warpInput; }
            return offset; 
        }
        private float TransferFuncFalling (float warpInput, float maxWarp, float warpRangeMin, float warpRangeMax)
        {
            float offset;
            if (warpInput > warpRangeMax) { offset = maxWarp; }
            else if (warpInput <= warpRangeMin) { offset = 0; }
            else { offset = warpInput - warpRangeMin; }
            return offset;
        }
    }
}
