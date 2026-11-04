
using UnityEngine;

public class UpperBodySway : MonoBehaviour
{
    public bool active;
    public float frequency = 2.5f;

    [Header("Pose - حالت ثابت هنگام جارو")]
    public Vector3 spinePose = new Vector3(20, 0, 0);
    public Vector3 chestPose = new Vector3(15, 0, 0);
    public Vector3 upperChestPose = new Vector3(10, 0, 0);
    public Vector3 rightArmPose;
    public Vector3 rightForeArmPose;
    public Vector3 leftArmPose;
    public Vector3 leftForeArmPose;

    [Header("Swing - حرکت رفت و برگشتی")]
    public Vector3 spineSwing = new Vector3(0, 12, 0);
    public Vector3 rightArmSwing;
    public Vector3 rightForeArmSwing;
    public Vector3 leftArmSwing;
    public Vector3 leftForeArmSwing;

    [Header("Left hand IK - فعلاً صفر بماند")]
    public Transform leftGrip;
    [Range(0f, 1f)] public float leftHandWeight = 0f;

    Animator anim;
    float t, w;
    Transform spine, chest, upper, rArm, rFore, lArm, lFore;

    void Awake() { anim = GetComponent<Animator>(); }

    void Start()
    {
        spine = anim.GetBoneTransform(HumanBodyBones.Spine);
        chest = anim.GetBoneTransform(HumanBodyBones.Chest);
        upper = anim.GetBoneTransform(HumanBodyBones.UpperChest);
        rArm = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
        rFore = anim.GetBoneTransform(HumanBodyBones.RightLowerArm);
        lArm = anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        lFore = anim.GetBoneTransform(HumanBodyBones.LeftLowerArm);
    }

    void LateUpdate()
    {
        w = Mathf.MoveTowards(w, active ? 1f : 0f, Time.deltaTime * 3f);
        if (w <= 0.001f) return;

        t += Time.deltaTime;
        float s = Mathf.Sin(t * frequency);

        Apply(spine, spinePose, spineSwing * 0.5f, s);
        Apply(chest, chestPose, spineSwing * 0.5f, s);
        Apply(upper, upperChestPose, Vector3.zero, s);
        Apply(rArm, rightArmPose, rightArmSwing, s);
        Apply(rFore, rightForeArmPose, rightForeArmSwing, s);
        Apply(lArm, leftArmPose, leftArmSwing, s);
        Apply(lFore, leftForeArmPose, leftForeArmSwing, s);
    }

    void Apply(Transform bone, Vector3 pose, Vector3 swing, float s)
    {
        if (!bone) return;
        bone.localRotation *= Quaternion.Euler((pose + swing * s) * w);
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (!anim || !leftGrip) return;
        float k = w * leftHandWeight;
        anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, k);
        anim.SetIKPosition(AvatarIKGoal.LeftHand, leftGrip.position);
    }
}