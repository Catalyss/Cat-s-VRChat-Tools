#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEditorInternal;
using System;
using System.Linq;
using VRC.SDK3.Avatars.Components;
using AnimatorController = UnityEditor.Animations.AnimatorController;
using VRC.SDK3.Avatars.ScriptableObjects;
using UnityEngine.SocialPlatforms;

[System.Serializable]
public class MultiToggleStruct
{
    public List<GameObject> Prefab;
    public bool IsASelector;
    public string ToggleName;
    public bool UseYAVOTCustomGUI;
    public string YAVOTCustomGUIName;
}
[System.Serializable]
public class MaterialSwapStruct
{
    public List<MaterialSelector> MaterialSelector = new List<MaterialSelector>();
    public bool IsASelector;
    public string ToggleName;
    public bool UseYAVOTCustomGUI;
    public string YAVOTCustomGUIName;
}
[System.Serializable]
public class MaterialSelector
{
    public SkinnedMeshRenderer MeshRenderer;
    public Material[] Base;
    public Material[] Swap;
}

public class MyScriptableObject : ScriptableObject
{

    [System.Serializable]
    public class ToggleTypeStruct
    {
        public enum ToggleType
        {
            MultiToggle = 0,
            MaterialSwap
        }
        public ToggleType Type = 0;
        public MultiToggleStruct MultiToggle = new MultiToggleStruct();
        public MaterialSwapStruct MaterialSwap = new MaterialSwapStruct();
    }
    public List<ToggleTypeStruct> ToggleTyped = new List<ToggleTypeStruct>();
}


public class AnimatorCreator : EditorWindow
{
    public static readonly string[] VrcParameters =
        {
            //VRC Defaults
            "IsLocal",
            "Viseme",
            "Voice",
            "GestureLeft",
            "GestureRight",
            "GestureLeftWeight",
            "GestureRightWeight",
            "AngularY",
            "VelocityX",
            "VelocityY",
            "VelocityZ",
            "VelocityMagnitude",
            "Upright",
            "Grounded",
            "Seated",
            "AFK",
            "TrackingType",
            "VRMode",
            "MuteSelf",
            "InStation",
            "Earmuffs",
            "Supine",
            "GroundProximity",
            "ScaleModified",
            "ScaleFactor",
            "ScaleFactorInverse",
            "EyeHeightAsMeters",
            "EyeHeightAsPercent",
            "IsOnFriendsList",
            "AvatarVersion",
        };
    static public GameObject VRCAvatarDescriptors;
    static public GameObject ModelHidden;
    static public Animator animator;
    static public bool SelectiveAviDisplay;
    static public AnimatorController animatorController;

    void RecursiveFindChild(Transform parent, AnimationCurve iDisableModelcurve, AnimationClip DisableModel)
    {
        foreach (Transform child in parent)
        {
            DisableModel.SetCurve(GetGameObjectPath(child.gameObject, VRCAvatarDescriptors), typeof(GameObject), "m_IsActive", iDisableModelcurve);
            RecursiveFindChild(child, iDisableModelcurve, DisableModel);
        }
    }

    void CreateController()
    {
        VRCAvatarDescriptors.GetComponent<VRCAvatarDescriptor>().customExpressions = true;
        VRCExpressionParameters newParameters = ScriptableObject.CreateInstance<VRCExpressionParameters>();
        newParameters.parameters = Array.Empty<VRCExpressionParameters.Parameter>();
        var parametersList = new List<VRCExpressionParameters.Parameter>();
        if (VRCAvatarDescriptors.GetComponent<VRCAvatarDescriptor>().expressionParameters == null)
        {
            AssetDatabase.CreateAsset(newParameters, "Assets" + "/" + VRCAvatarDescriptors.name + ".asset");
        }
        else
        {
            string path = (AssetDatabase.GetAssetPath(VRCAvatarDescriptors.GetComponent<VRCAvatarDescriptor>().expressionParameters));
            newParameters = AssetDatabase.LoadAssetAtPath(path, typeof(VRCExpressionParameters)) as VRCExpressionParameters;
        }
        if (!AssetDatabase.IsValidFolder("Assets" + "/" + VRCAvatarDescriptors.name)) AssetDatabase.CreateFolder("Assets", VRCAvatarDescriptors.name);
        if (!AssetDatabase.IsValidFolder("Assets" + "/" + VRCAvatarDescriptors.name + "/Animation")) AssetDatabase.CreateFolder("Assets" + "/" + VRCAvatarDescriptors.name, "Animation");

        AnimationClip DisableModel = new AnimationClip();
        if (SelectiveAviDisplay)
        {
            Keyframe[] DisableModelkeys;
            DisableModelkeys = new Keyframe[2];
            DisableModelkeys[0] = new Keyframe(0.0f, 1f);
            DisableModelkeys[1] = new Keyframe(1.0f / 60f, 0f);
            AnimationCurve DisableModelcurve = new AnimationCurve(DisableModelkeys);

            Keyframe[] iDisableModelkeys;
            iDisableModelkeys = new Keyframe[2];
            iDisableModelkeys[0] = new Keyframe(0.0f, 0f);
            iDisableModelkeys[1] = new Keyframe(1.0f / 60f, 1f);
            AnimationCurve iDisableModelcurve = new AnimationCurve(iDisableModelkeys);

            DisableModel.SetCurve(GetGameObjectPath(ModelHidden, VRCAvatarDescriptors), typeof(GameObject), "m_IsActive", iDisableModelcurve);
            RecursiveFindChild(ModelHidden.transform, iDisableModelcurve, DisableModel);

            foreach (var gameObject in VRCAvatarDescriptors.transform.GetComponentsInChildren<MeshRenderer>())
            {
                if (gameObject.transform.IsChildOf(ModelHidden.transform)) continue;
                DisableModel.SetCurve(GetGameObjectPath(gameObject.gameObject, VRCAvatarDescriptors), typeof(GameObject), "m_IsActive", DisableModelcurve);
            }
            foreach (var gameObject in VRCAvatarDescriptors.transform.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (gameObject.transform.IsChildOf(ModelHidden.transform)) continue;
                DisableModel.SetCurve(GetGameObjectPath(gameObject.gameObject, VRCAvatarDescriptors), typeof(GameObject), "m_IsActive", DisableModelcurve);
            }
            AnimationUtility.GetAnimationClipSettings(DisableModel).loopTime = false;
            DisableModel.name = "DisableModel";

            AssetDatabase.CreateAsset(DisableModel, "Assets" + "/" + VRCAvatarDescriptors.name + "/Animation/" + DisableModel.name + ".anim");

            string path = ("Assets" + "/" + VRCAvatarDescriptors.name + "/Animation/" + DisableModel.name + ".anim");
            DisableModel = AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip)) as AnimationClip;

            List<string> parmname = new List<string>();
            foreach (var parm in animatorController.parameters.ToList<UnityEngine.AnimatorControllerParameter>())
            {
                parmname.Add(parm.name);
            }
            if (!parmname.Contains("--Default Ignore--")) animatorController.AddParameter("--Default Ignore--", UnityEngine.AnimatorControllerParameterType.Trigger);
            if (!parmname.Contains("IsLocal")) animatorController.AddParameter("IsLocal", UnityEngine.AnimatorControllerParameterType.Bool);
            if (!parmname.Contains("IsOnFriendsList")) animatorController.AddParameter("IsOnFriendsList", UnityEngine.AnimatorControllerParameterType.Bool);
            if (!parmname.Contains("IsUserAllowed")) animatorController.AddParameter("IsUserAllowed", UnityEngine.AnimatorControllerParameterType.Bool);
            if (!parmname.Contains("--Method Start--")) animatorController.AddParameter("--Method Start--", UnityEngine.AnimatorControllerParameterType.Trigger);
            if (!parmname.Contains("Friend Allowed")) animatorController.AddParameter("Friend Allowed", UnityEngine.AnimatorControllerParameterType.Bool);
            if (!parmname.Contains("Friend Toggle")) animatorController.AddParameter("Friend Toggle", UnityEngine.AnimatorControllerParameterType.Bool);
            if (!parmname.Contains("Global Allowed")) animatorController.AddParameter("Global Allowed", UnityEngine.AnimatorControllerParameterType.Bool);
            if (!parmname.Contains("Global Toggle")) animatorController.AddParameter("Global Toggle", UnityEngine.AnimatorControllerParameterType.Bool);
            if (!parmname.Contains("Local Allowed")) animatorController.AddParameter("Local Allowed", UnityEngine.AnimatorControllerParameterType.Bool);
            if (!parmname.Contains("Local Toggle")) animatorController.AddParameter("Local Toggle", UnityEngine.AnimatorControllerParameterType.Bool);
            if (!parmname.Contains("--Method End--")) animatorController.AddParameter("--Method End--", UnityEngine.AnimatorControllerParameterType.Trigger);
            for (int i = 0; i < 8; i++)
            {
                if (animatorController.parameters[animatorController.parameters.Length - 8 + i].name.Contains("--")) continue;
                var parm = new VRCExpressionParameters.Parameter
                {
                    name = animatorController.parameters[animatorController.parameters.Length - 8 + i].name,
                    networkSynced = true,
                    saved = animatorController.parameters[animatorController.parameters.Length - 8 + i].name.Contains(" Allowed"),
                    valueType = VRCExpressionParameters.ValueType.Bool,
                    defaultValue = 0
                };

                if (newParameters.FindParameter(parm.name) == null)
                {
                    parametersList.Add(parm);
                }
            }
            var layer = new UnityEditor.Animations.AnimatorControllerLayer
            {
                name = "Managed Allowance",
                defaultWeight = 1f,
                stateMachine = new UnityEditor.Animations.AnimatorStateMachine()
            };
            animatorController.AddLayer(layer);
            if (AssetDatabase.GetAssetPath(animatorController) != "") AssetDatabase.AddObjectToAsset(layer.stateMachine, AssetDatabase.GetAssetPath(animatorController));

            var DisableModelState = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddState("Disable Model");
            var GlobalModelState = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddState("Global Display ON");
            var FriendModelState = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddState("Friend Display ON");
            var LocalModelState = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddState("Local Display ON");

            DisableModelState.motion = DisableModel;
            GlobalModelState.motion = DisableModel;
            FriendModelState.motion = DisableModel;
            LocalModelState.motion = DisableModel;

            GlobalModelState.speed = -1f;
            FriendModelState.speed = -1f;
            LocalModelState.speed = -1f;

            var AnyToDisableA = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(DisableModelState);
            var AnyToDisableAA = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(DisableModelState);
            var AnyToDisableAAA = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(DisableModelState);
            var AnyToDisableB = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(GlobalModelState);
            var AnyToDisableBB = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(GlobalModelState);
            var AnyToDisableC = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(FriendModelState);
            var AnyToDisableCC = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(FriendModelState);
            var AnyToDisableD = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(LocalModelState);

            AnyToDisableA.AddCondition(AnimatorConditionMode.If, 0, "IsLocal");
            AnyToDisableA.AddCondition(AnimatorConditionMode.IfNot, 0, "Local Allowed");
            AnyToDisableA.duration = 0.1f;
            AnyToDisableA.canTransitionToSelf = false;

            AnyToDisableAA.AddCondition(AnimatorConditionMode.IfNot, 0, "IsLocal");
            AnyToDisableAA.AddCondition(AnimatorConditionMode.IfNot, 0, "IsOnFriendsList");
            AnyToDisableAA.AddCondition(AnimatorConditionMode.IfNot, 0, "IsUserAllowed");
            AnyToDisableAA.AddCondition(AnimatorConditionMode.IfNot, 0, "Global Allowed");
            AnyToDisableAA.duration = 0.1f;
            AnyToDisableAA.canTransitionToSelf = false;

            AnyToDisableAAA.AddCondition(AnimatorConditionMode.If, 0, "IsOnFriendsList");
            AnyToDisableAAA.AddCondition(AnimatorConditionMode.IfNot, 0, "IsUserAllowed");
            AnyToDisableAAA.AddCondition(AnimatorConditionMode.IfNot, 0, "Friend Allowed");
            AnyToDisableAAA.duration = 0.1f;
            AnyToDisableAAA.canTransitionToSelf = false;



            AnyToDisableB.AddCondition(AnimatorConditionMode.IfNot, 0, "IsLocal");
            AnyToDisableB.AddCondition(AnimatorConditionMode.IfNot, 0, "IsOnFriendsList");
            AnyToDisableB.AddCondition(AnimatorConditionMode.If, 0, "IsUserAllowed");
            AnyToDisableB.AddCondition(AnimatorConditionMode.IfNot, 0, "Global Allowed");
            AnyToDisableB.duration = 0.1f;
            AnyToDisableB.canTransitionToSelf = false;

            AnyToDisableBB.AddCondition(AnimatorConditionMode.IfNot, 0, "IsLocal");
            AnyToDisableBB.AddCondition(AnimatorConditionMode.IfNot, 0, "IsOnFriendsList");
            AnyToDisableBB.AddCondition(AnimatorConditionMode.IfNot, 0, "IsUserAllowed");
            AnyToDisableBB.AddCondition(AnimatorConditionMode.If, 0, "Global Allowed");
            AnyToDisableBB.duration = 0.1f;
            AnyToDisableBB.canTransitionToSelf = false;



            AnyToDisableC.AddCondition(AnimatorConditionMode.If, 0, "IsOnFriendsList");
            AnyToDisableC.AddCondition(AnimatorConditionMode.IfNot, 0, "Friend Allowed");
            AnyToDisableC.AddCondition(AnimatorConditionMode.If, 0, "IsUserAllowed");
            AnyToDisableC.duration = 0.1f;
            AnyToDisableC.canTransitionToSelf = false;

            AnyToDisableCC.AddCondition(AnimatorConditionMode.If, 0, "IsOnFriendsList");
            AnyToDisableCC.AddCondition(AnimatorConditionMode.If, 0, "Friend Allowed");
            AnyToDisableCC.AddCondition(AnimatorConditionMode.IfNot, 0, "IsUserAllowed");
            AnyToDisableCC.duration = 0.1f;
            AnyToDisableCC.canTransitionToSelf = false;



            AnyToDisableD.AddCondition(AnimatorConditionMode.If, 0, "IsLocal");
            AnyToDisableD.AddCondition(AnimatorConditionMode.If, 0, "Local Allowed");
            AnyToDisableD.duration = 0.1f;
            AnyToDisableD.canTransitionToSelf = false;



        }
        List<string> ToggleNameList = new List<string>();
        for (int index = 0; index < ro_list.count; index++)
        {
            SerializedProperty element = ro_list.serializedProperty.GetArrayElementAtIndex(index);
            if (element.FindPropertyRelative("Type").enumNames[ro_list.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Type").enumValueIndex] == "MultiToggle")
            {
                element = element.FindPropertyRelative("MultiToggle");
                string ToggleName = element.FindPropertyRelative("ToggleName").stringValue;
                string YAVOTGUI = element.FindPropertyRelative("YAVOTCustomGUIName").stringValue;
                bool IsYavot = element.FindPropertyRelative("UseYAVOTCustomGUI").boolValue;
                List<GameObject> Object = new List<GameObject>();
                for (int obj = 0; obj < element.FindPropertyRelative("Prefab").arraySize; obj++)
                {
                    Object.Add((GameObject)element.FindPropertyRelative("Prefab").GetArrayElementAtIndex(obj).objectReferenceValue);
                }

                IsYavot = false;

                if (Object.Count == 0) { Debug.Log("Toggle n°" + index + " is empty and will be skipped"); continue; }
                if (ToggleName == "") ToggleName = Object[0].name;

                string ParmName = ToggleName;

                if (IsYavot)
                {
                    //YAVOTCustomGUI°UISET°0-5
                    if (YAVOTGUI == "") YAVOTGUI = "UnnamedGUI";
                    ParmName = "YAVOTCustomGUI" + "\\u00b0" + "SET" + YAVOTGUI + "\\u00b0" + ToggleName;
                }

                if (element.FindPropertyRelative("IsASelector").boolValue) { SelectorAnimation(Object, ToggleName, ParmName, DisableModel, newParameters, parametersList); continue; }

                AnimationClip clip = new AnimationClip();
                Keyframe[] keys;
                keys = new Keyframe[2];
                keys[0] = new Keyframe(0.0f, 0.0f);
                keys[1] = new Keyframe(1.0f / 60f, 1f);
                AnimationCurve curve = new AnimationCurve(keys);
                foreach (GameObject item in Object) { clip.SetCurve(GetGameObjectPath(item, VRCAvatarDescriptors), typeof(GameObject), "m_IsActive", curve); }
                AnimationUtility.GetAnimationClipSettings(clip).loopTime = false;
                clip.name = ToggleName + index;

                AssetDatabase.CreateAsset(clip, "Assets" + "/" + VRCAvatarDescriptors.name + "/Animation/" + clip.name + ".anim");

                string path = ("Assets" + "/" + VRCAvatarDescriptors.name + "/Animation/" + clip.name + ".anim");
                clip = AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip)) as AnimationClip;

                AnimatorManipulation(clip, ToggleName, ParmName, DisableModel, newParameters, parametersList);
                continue;
            }
            else if (element.FindPropertyRelative("Type").enumNames[ro_list.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Type").enumValueIndex] == "MaterialSwap")
            {
                element = element.FindPropertyRelative("MaterialSwap");
                string ToggleName = element.FindPropertyRelative("ToggleName").stringValue;
                string YAVOTGUI = element.FindPropertyRelative("YAVOTCustomGUIName").stringValue;
                bool IsYavot = element.FindPropertyRelative("UseYAVOTCustomGUI").boolValue;

                List<MaterialSelector> Object = new List<MaterialSelector>();
                for (int obj = 0; obj < element.FindPropertyRelative("MaterialSelector").arraySize; obj++)
                {
                    MaterialSelector MS = new MaterialSelector
                    {
                        MeshRenderer = (SkinnedMeshRenderer)element.FindPropertyRelative("MaterialSelector").GetArrayElementAtIndex(obj).FindPropertyRelative("MeshRenderer").objectReferenceValue
                    };
                    List<Material> MatList = new List<Material>();
                    List<Material> MatListSwap = new List<Material>();
                    for (int bases = 0; bases < element.FindPropertyRelative("MaterialSelector").GetArrayElementAtIndex(obj).FindPropertyRelative("Base").arraySize; bases++)
                    {
                        MatList.Add((Material)element.FindPropertyRelative("MaterialSelector").GetArrayElementAtIndex(obj).FindPropertyRelative("Base").GetArrayElementAtIndex(bases).objectReferenceValue);
                        MatListSwap.Add((Material)element.FindPropertyRelative("MaterialSelector").GetArrayElementAtIndex(obj).FindPropertyRelative("Swap").GetArrayElementAtIndex(bases).objectReferenceValue);
                    }
                    MS.Base = MatList.ToArray();
                    MS.Swap = MatListSwap.ToArray();
                    Object.Add(MS);
                }

                IsYavot = false;

                if (Object.Count == 0) { Debug.Log("Toggle n°" + index + " is empty and will be skipped"); continue; }
                if (ToggleName == "") ToggleName = Object[0].MeshRenderer.name;

                string ParmName = ToggleName;

                if (IsYavot)
                {
                    //YAVOTCustomGUI°UISET°0-5
                    if (YAVOTGUI == "") YAVOTGUI = "UnnamedGUI";
                    ParmName = "YAVOTCustomGUI" + "\\u00b0" + "SET" + YAVOTGUI + "\\u00b0" + ToggleName;
                }

                if (element.FindPropertyRelative("IsASelector").boolValue)
                {
                    SelectorSwapAnimation(Object, ToggleName, ParmName, DisableModel, newParameters, parametersList); continue;
                }

                AnimationClip clip = new AnimationClip();


                foreach (MaterialSelector item in Object)
                {
                    for (int i = 0; i < item.Swap.Length; i++)
                    {
                        ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[2];

                        EditorCurveBinding curveBinding = EditorCurveBinding.PPtrCurve(GetGameObjectPath(item.MeshRenderer.gameObject, VRCAvatarDescriptors), typeof(SkinnedMeshRenderer), "m_Materials.Array.data[" + i + "]");
                        keyFrames[0] = new ObjectReferenceKeyframe
                        {
                            time = 0f,
                            value = item.Swap[i]
                        };
                        keyFrames[1] = new ObjectReferenceKeyframe
                        {
                            time = 1 / 60f,
                            value = item.Base[i]
                        };
                        AnimationUtility.SetObjectReferenceCurve(clip, curveBinding, keyFrames);
                    }
                }

                AnimationUtility.GetAnimationClipSettings(clip).loopTime = false;
                clip.name = ToggleName + index;

                AssetDatabase.CreateAsset(clip, "Assets" + "/" + VRCAvatarDescriptors.name + "/Animation/" + clip.name + ".anim");

                string path = ("Assets" + "/" + VRCAvatarDescriptors.name + "/Animation/" + clip.name + ".anim");
                clip = AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip)) as AnimationClip;

                AnimatorManipulation(clip, ToggleName, ParmName, DisableModel, newParameters, parametersList);
                continue;
            }
        }
        newParameters.parameters = parametersList.ToArray();
        VRCAvatarDescriptors.GetComponent<VRCAvatarDescriptor>().expressionParameters = newParameters;
        OnEnable();
    }

    public void AnimatorManipulation(AnimationClip clip, string ToggleName, string ParmName, AnimationClip DisableModel, VRCExpressionParameters newParameters, List<VRCExpressionParameters.Parameter> parametersList)
    {
        if (SelectiveAviDisplay)
        {
            var layer = new UnityEditor.Animations.AnimatorControllerLayer
            {
                name = ToggleName,
                defaultWeight = 1f,
                stateMachine = new UnityEditor.Animations.AnimatorStateMachine()
            };

            animatorController.AddLayer(layer);
            if (AssetDatabase.GetAssetPath(animatorController) != "") AssetDatabase.AddObjectToAsset(layer.stateMachine, AssetDatabase.GetAssetPath(animatorController));
            animatorController.AddParameter(ParmName, UnityEngine.AnimatorControllerParameterType.Bool);

            var parm = new VRCExpressionParameters.Parameter
            {
                name = animatorController.parameters[animatorController.parameters.Length - 1].name,
                networkSynced = true,
                saved = true,
                valueType = VRCExpressionParameters.ValueType.Bool,
                defaultValue = 0
            };

            if (newParameters.FindParameter(parm.name) == null)
            {
                parametersList.Add(parm);
            }


            SeletorDisplaySetup(clip, DisableModel);

            //State setup
            //
            //

            SelectiveAddClip(clip);
            
        }
        else
        {
            var layer = new UnityEditor.Animations.AnimatorControllerLayer
            {
                name = ToggleName,
                defaultWeight = 1f,
                stateMachine = new UnityEditor.Animations.AnimatorStateMachine()
            };

            animatorController.AddLayer(layer);
            animatorController.AddParameter(ParmName, UnityEngine.AnimatorControllerParameterType.Bool);

            
            var parm = new VRCExpressionParameters.Parameter
            {
                name = animatorController.parameters[animatorController.parameters.Length - 1].name,
                networkSynced = true,
                saved = true,
                valueType = VRCExpressionParameters.ValueType.Bool,
                defaultValue = 0
            };

            if (newParameters.FindParameter(parm.name) == null)
            {
                parametersList.Add(parm);
            }

            if (AssetDatabase.GetAssetPath(animatorController) != "") AssetDatabase.AddObjectToAsset(layer.stateMachine, AssetDatabase.GetAssetPath(animatorController));
            var OnState = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddState(ToggleName + " On");
            var OffState = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddState(ToggleName + " Off");
            OnState.motion = clip;
            OffState.motion = clip;
            OffState.speed = -1f;

            var OnToOff = OnState.AddTransition(OffState);
            OnToOff.AddCondition(AnimatorConditionMode.If, 0, animatorController.parameters[animatorController.parameters.Length - 1].name);
            OnToOff.duration = 0.1f;
            var OffToOn = OffState.AddTransition(OnState);
            OffToOn.AddCondition(AnimatorConditionMode.IfNot, 0, animatorController.parameters[animatorController.parameters.Length - 1].name);
            OffToOn.duration = 0.1f;

            animatorController.layers[animatorController.layers.Length - 1].stateMachine.defaultState = OnState;

        }

    }
    public void SelectorAnimation(List<GameObject> gameObjects, string ToggleName, string ParmName, AnimationClip DisableModel, VRCExpressionParameters newParameters, List<VRCExpressionParameters.Parameter> parametersList)
    {



        Keyframe[] keys;
        keys = new Keyframe[2];
        keys[0] = new Keyframe(0.0f, 0.0f);
        keys[1] = new Keyframe(1.0f / 60f, 1f);
        AnimationCurve curve = new AnimationCurve(keys);

        Keyframe[] Inversekeys;
        Inversekeys = new Keyframe[2];
        Inversekeys[0] = new Keyframe(0.0f, 1f);
        Inversekeys[1] = new Keyframe(1.0f / 60f, 0f);
        AnimationCurve Inversecurve = new AnimationCurve(Inversekeys);

        var layer = new UnityEditor.Animations.AnimatorControllerLayer
        {
            name = ToggleName,
            defaultWeight = 1f,
            stateMachine = new UnityEditor.Animations.AnimatorStateMachine()
        };

        animatorController.AddLayer(layer);

        var parm = new VRCExpressionParameters.Parameter
        {
            name = animatorController.parameters[animatorController.parameters.Length - 1].name,
            networkSynced = true,
            saved = true,
            valueType = VRCExpressionParameters.ValueType.Int,
            defaultValue = 0
        };

        if (newParameters.FindParameter(parm.name) == null)
        {
            parametersList.Add(parm);
        }
        if (AssetDatabase.GetAssetPath(animatorController) != "") AssetDatabase.AddObjectToAsset(layer.stateMachine, AssetDatabase.GetAssetPath(animatorController));
        animatorController.AddParameter(ParmName, UnityEngine.AnimatorControllerParameterType.Int);
        if (SelectiveAviDisplay)
        {
            AnimationClip clip = new AnimationClip();
            foreach (GameObject item in gameObjects)
            {
                if (item == gameObjects[0]) clip.SetCurve(GetGameObjectPath(item, VRCAvatarDescriptors), typeof(GameObject), "m_IsActive", Inversecurve);
                else clip.SetCurve(GetGameObjectPath(item, VRCAvatarDescriptors), typeof(GameObject), "m_IsActive", curve);
            }

            clip.name = gameObjects[0].name + "Index";
            AssetDatabase.CreateAsset(clip, "Assets" + "/" + VRCAvatarDescriptors.name + "/Animation/" + clip.name + ".anim");

            string path = ("Assets" + "/" + VRCAvatarDescriptors.name + "/Animation/" + clip.name + ".anim");
            clip = AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip)) as AnimationClip;

            SelectiveSelector(DisableModel, gameObjects, curve, Inversecurve, clip);
            return;
        }
        for (int index = 0; index < gameObjects.Count; index++)
        {
            AnimationClip clip = new AnimationClip();

            foreach (GameObject item in gameObjects)
            {
                if (item == gameObjects[index]) clip.SetCurve(GetGameObjectPath(item, VRCAvatarDescriptors), typeof(GameObject), "m_IsActive", Inversecurve);
                else clip.SetCurve(GetGameObjectPath(item, VRCAvatarDescriptors), typeof(GameObject), "m_IsActive", curve);
            }
            clip.name = gameObjects[index].name + " index " + index;
            AssetDatabase.CreateAsset(clip, "Assets" + "/" + VRCAvatarDescriptors.name + "/Animation/" + clip.name + ".anim");

            string path = ("Assets" + "/" + VRCAvatarDescriptors.name + "/Animation/" + clip.name + ".anim");
            clip = AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip)) as AnimationClip;

            var State = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddState(gameObjects[index].name + " index " + index);
            var AnyToState = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(State);

            State.motion = clip;

            AnyToState.AddCondition(AnimatorConditionMode.Equals, index, animatorController.parameters[animatorController.parameters.Length - 1].name);
            AnyToState.duration = 0.1f;
            AnyToState.canTransitionToSelf = false;
        }
        {
            AnimationClip clipoff = new AnimationClip();
            foreach (GameObject item in gameObjects)
            {
                clipoff.SetCurve(GetGameObjectPath(item, VRCAvatarDescriptors), typeof(GameObject), "m_IsActive", curve);
            }

            var StateOff = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddState("All Off index " + (gameObjects.Count + 2));
            var AnyToStateOff = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(StateOff);

            clipoff.name = "All Off index " + gameObjects.Count + 2;
            AssetDatabase.CreateAsset(clipoff, "Assets" + "/" + VRCAvatarDescriptors.name + "/Animation/" + clipoff.name + ".anim");

            string path = ("Assets" + "/" + VRCAvatarDescriptors.name + "/Animation/" + clipoff.name + ".anim");
            clipoff = AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip)) as AnimationClip;

            StateOff.motion = clipoff;

            AnyToStateOff.AddCondition(AnimatorConditionMode.Equals, gameObjects.Count + 2, animatorController.parameters[animatorController.parameters.Length - 1].name);
            AnyToStateOff.duration = 0.1f;
            AnyToStateOff.canTransitionToSelf = false;
        }

    }
    public void SelectorSwapAnimation(List<MaterialSelector> objects, string ToggleName, string ParmName, AnimationClip DisableModel, VRCExpressionParameters newParameters, List<VRCExpressionParameters.Parameter> parametersList)
    {




        var layer = new UnityEditor.Animations.AnimatorControllerLayer
        {
            name = ToggleName,
            defaultWeight = 1f,
            stateMachine = new UnityEditor.Animations.AnimatorStateMachine()
        };

        animatorController.AddLayer(layer);

        var parm = new VRCExpressionParameters.Parameter
        {
            name = animatorController.parameters[animatorController.parameters.Length - 1].name,
            networkSynced = true,
            saved = true,
            valueType = VRCExpressionParameters.ValueType.Int,
            defaultValue = 0
        };

        if (newParameters.FindParameter(parm.name) == null)
        {
            parametersList.Add(parm);
        }

        if (AssetDatabase.GetAssetPath(animatorController) != "") AssetDatabase.AddObjectToAsset(layer.stateMachine, AssetDatabase.GetAssetPath(animatorController));
        animatorController.AddParameter(ParmName, UnityEngine.AnimatorControllerParameterType.Int);
        if (SelectiveAviDisplay)
        {
            AnimationClip clip = new AnimationClip();
            foreach (MaterialSelector item in objects)
            {
                for (int i = 0; i < item.Swap.Length; i++)
                {
                    ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[2];
                    EditorCurveBinding curveBinding = EditorCurveBinding.PPtrCurve(GetGameObjectPath(item.MeshRenderer.gameObject, VRCAvatarDescriptors), typeof(SkinnedMeshRenderer), "m_Materials.Array.data[" + i + "]");
                    keyFrames[0] = new ObjectReferenceKeyframe
                    {
                        time = 0f,
                        value = item.Swap[i]
                    };
                    keyFrames[1] = new ObjectReferenceKeyframe
                    {
                        time = 1 / 60f,
                        value = item.Base[i]
                    };
                    AnimationUtility.SetObjectReferenceCurve(clip, curveBinding, keyFrames);
                }
            }

            clip.name = objects[0].MeshRenderer.gameObject.name + "Index";
            AssetDatabase.CreateAsset(clip, "Assets" + "/" + VRCAvatarDescriptors.name + "/Animation/" + clip.name + ".anim");

            string path = ("Assets" + "/" + VRCAvatarDescriptors.name + "/Animation/" + clip.name + ".anim");
            clip = AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip)) as AnimationClip;

            SelectiveSwapSelector(DisableModel, objects, clip);
            return;
        }

        for (int index = 0; index < objects.Count; index++)
        {
            AnimationClip clip = new AnimationClip();
            foreach (MaterialSelector item in objects)
            {
                if (item.MeshRenderer != objects[index].MeshRenderer)
                {
                    for (int i = 0; i < item.Swap.Length; i++)
                    {
                        ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[2];

                        EditorCurveBinding curveBinding = EditorCurveBinding.PPtrCurve(GetGameObjectPath(item.MeshRenderer.gameObject, VRCAvatarDescriptors), typeof(SkinnedMeshRenderer), "m_Materials.Array.data[" + i + "]");
                        keyFrames[0] = new ObjectReferenceKeyframe
                        {
                            time = 0f,
                            value = item.Swap[i]
                        };
                        keyFrames[1] = new ObjectReferenceKeyframe
                        {
                            time = 1 / 60f,
                            value = item.Base[i]
                        };
                        AnimationUtility.SetObjectReferenceCurve(clip, curveBinding, keyFrames);
                    }
                }
                else
                {
                    for (int i = 0; i < item.Swap.Length; i++)
                    {
                        ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[2];

                        EditorCurveBinding curveBinding = EditorCurveBinding.PPtrCurve(GetGameObjectPath(item.MeshRenderer.gameObject, VRCAvatarDescriptors), typeof(SkinnedMeshRenderer), "m_Materials.Array.data[" + i + "]");
                        keyFrames[0] = new ObjectReferenceKeyframe
                        {
                            time = 0f,
                            value = item.Base[i]
                        };
                        keyFrames[1] = new ObjectReferenceKeyframe
                        {
                            time = 1 / 60f,
                            value = item.Swap[i]
                        };
                        AnimationUtility.SetObjectReferenceCurve(clip, curveBinding, keyFrames);
                    }
                }
            }
            clip.name = objects[index].MeshRenderer.gameObject.name + " index " + index;
            AssetDatabase.CreateAsset(clip, "Assets" + "/" + VRCAvatarDescriptors.name + "/Animation/" + clip.name + ".anim");

            string path = ("Assets" + "/" + VRCAvatarDescriptors.name + "/Animation/" + clip.name + ".anim");
            clip = AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip)) as AnimationClip;

            var State = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddState(objects[index].MeshRenderer.gameObject.name + " index " + index);
            var AnyToState = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(State);

            State.motion = clip;

            AnyToState.AddCondition(AnimatorConditionMode.Equals, index, animatorController.parameters[animatorController.parameters.Length - 1].name);
            AnyToState.duration = 0.1f;
            AnyToState.canTransitionToSelf = false;
        }
        {
            AnimationClip clipoff = new AnimationClip();
            foreach (MaterialSelector item in objects)
            {
                for (int i = 0; i < item.Swap.Length; i++)
                {
                    ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[2];
                    EditorCurveBinding curveBinding = EditorCurveBinding.PPtrCurve(GetGameObjectPath(item.MeshRenderer.gameObject, VRCAvatarDescriptors), typeof(SkinnedMeshRenderer), "m_Materials.Array.data[" + i + "]");
                    keyFrames[0] = new ObjectReferenceKeyframe
                    {
                        time = 0f,
                        value = item.Base[i]
                    };
                    keyFrames[1] = new ObjectReferenceKeyframe
                    {
                        time = 1 / 60f,
                        value = item.Swap[i]
                    };
                    AnimationUtility.SetObjectReferenceCurve(clipoff, curveBinding, keyFrames);
                    curveBinding = EditorCurveBinding.PPtrCurve(GetGameObjectPath(item.MeshRenderer.gameObject, VRCAvatarDescriptors), typeof(SkinnedMeshRenderer), "m_Materials.Array.data[" + i + "].m_FileID");
                    AnimationUtility.SetObjectReferenceCurve(clipoff, curveBinding, keyFrames);
                    curveBinding = EditorCurveBinding.PPtrCurve(GetGameObjectPath(item.MeshRenderer.gameObject, VRCAvatarDescriptors), typeof(SkinnedMeshRenderer), "m_Materials.Array.data[" + i + "].m_PathID");
                    AnimationUtility.SetObjectReferenceCurve(clipoff, curveBinding, keyFrames);
                }
            }

            var StateOff = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddState("All Off index " + (objects.Count + 2));
            var AnyToStateOff = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(StateOff);

            clipoff.name = "All Swap index " + (objects.Count + 2);
            AssetDatabase.CreateAsset(clipoff, "Assets" + "/" + VRCAvatarDescriptors.name + "/Animation/" + clipoff.name + ".anim");

            string path = ("Assets" + "/" + VRCAvatarDescriptors.name + "/Animation/" + clipoff.name + ".anim");
            clipoff = AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip)) as AnimationClip;

            StateOff.motion = clipoff;

            AnyToStateOff.AddCondition(AnimatorConditionMode.Equals, objects.Count + 2, animatorController.parameters[animatorController.parameters.Length - 1].name);
            AnyToStateOff.duration = 0.1f;
            AnyToStateOff.canTransitionToSelf = false;
        }
        {
            AnimationClip clipoff = new AnimationClip();
            foreach (MaterialSelector item in objects)
            {
                for (int i = 0; i < item.Swap.Length; i++)
                {
                    ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[2];
                    EditorCurveBinding curveBinding = EditorCurveBinding.PPtrCurve(GetGameObjectPath(item.MeshRenderer.gameObject, VRCAvatarDescriptors), typeof(SkinnedMeshRenderer), "m_Materials.Array.data[" + i + "]");
                    keyFrames[0] = new ObjectReferenceKeyframe
                    {
                        time = 0f,
                        value = item.Swap[i]
                    };
                    keyFrames[1] = new ObjectReferenceKeyframe
                    {
                        time = 1 / 60f,
                        value = item.Base[i]
                    };
                    AnimationUtility.SetObjectReferenceCurve(clipoff, curveBinding, keyFrames);
                    curveBinding = EditorCurveBinding.PPtrCurve(GetGameObjectPath(item.MeshRenderer.gameObject, VRCAvatarDescriptors), typeof(SkinnedMeshRenderer), "m_Materials.Array.data[" + i + "].m_FileID");
                    AnimationUtility.SetObjectReferenceCurve(clipoff, curveBinding, keyFrames);
                    curveBinding = EditorCurveBinding.PPtrCurve(GetGameObjectPath(item.MeshRenderer.gameObject, VRCAvatarDescriptors), typeof(SkinnedMeshRenderer), "m_Materials.Array.data[" + i + "].m_PathID");
                    AnimationUtility.SetObjectReferenceCurve(clipoff, curveBinding, keyFrames);
                }
            }

            var StateOff = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddState("All Off index " + (objects.Count + 3));
            var AnyToStateOff = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(StateOff);

            clipoff.name = "All Base index " + (objects.Count + 3);
            AssetDatabase.CreateAsset(clipoff, "Assets" + "/" + VRCAvatarDescriptors.name + "/Animation/" + clipoff.name + ".anim");

            string path = ("Assets" + "/" + VRCAvatarDescriptors.name + "/Animation/" + clipoff.name + ".anim");
            clipoff = AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip)) as AnimationClip;

            StateOff.motion = clipoff;

            AnyToStateOff.AddCondition(AnimatorConditionMode.Equals, objects.Count + 3, animatorController.parameters[animatorController.parameters.Length - 1].name);
            AnyToStateOff.duration = 0.1f;
            AnyToStateOff.canTransitionToSelf = false;
        }

    }

    public void SelectiveSelector(AnimationClip DisableModel, List<GameObject> gameObjects, AnimationCurve curve, AnimationCurve Inversecurve, AnimationClip baseclip)
    {
        SeletorDisplaySetup(baseclip, DisableModel);
        // Selector Setup
        //
        //

        for (int index = 0; index < gameObjects.Count; index++)
        {
            AnimationClip clip = new AnimationClip();

            foreach (GameObject item in gameObjects)
            {
                if (item == gameObjects[index]) clip.SetCurve(GetGameObjectPath(item, VRCAvatarDescriptors), typeof(GameObject), "m_IsActive", Inversecurve);
                else clip.SetCurve(GetGameObjectPath(item, VRCAvatarDescriptors), typeof(GameObject), "m_IsActive", curve);
            }
            clip.name = gameObjects[index].name + "Selective " + index;
            AssetDatabase.CreateAsset(clip, "Assets" + "/" + VRCAvatarDescriptors.name + "/Animation/" + clip.name + ".anim");

            string path = ("Assets" + "/" + VRCAvatarDescriptors.name + "/Animation/" + clip.name + ".anim");
            clip = AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip)) as AnimationClip;
            SelectorAddClip(clip, index);

        }

        {
            AnimationClip clipoff = new AnimationClip();
            foreach (GameObject item in gameObjects)
            {
                clipoff.SetCurve(GetGameObjectPath(item, VRCAvatarDescriptors), typeof(GameObject), "m_IsActive", curve);
            }
            clipoff.name = "All Off index " + gameObjects.Count + 2;
            AssetDatabase.CreateAsset(clipoff, "Assets" + "/" + VRCAvatarDescriptors.name + "/Animation/" + clipoff.name + ".anim");

            string path = ("Assets" + "/" + VRCAvatarDescriptors.name + "/Animation/" + clipoff.name + ".anim");
            clipoff = AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip)) as AnimationClip;
            SelectorAddClip(clipoff, gameObjects.Count + 2);
        }

    }
    public void SelectiveSwapSelector(AnimationClip DisableModel, List<MaterialSelector> objects, AnimationClip baseclip)
    {
        SeletorDisplaySetup(baseclip, DisableModel);
        // Selector Setup
        //
        //

        for (int index = 0; index < objects.Count; index++)
        {
            AnimationClip clip = new AnimationClip();

            foreach (MaterialSelector item in objects)
            {
                if (item.MeshRenderer != objects[index].MeshRenderer)
                {
                    for (int i = 0; i < item.Swap.Length; i++)
                    {
                        ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[2];

                        EditorCurveBinding curveBinding = EditorCurveBinding.PPtrCurve(GetGameObjectPath(item.MeshRenderer.gameObject, VRCAvatarDescriptors), typeof(SkinnedMeshRenderer), "m_Materials.Array.data[" + i + "]");
                        keyFrames[0] = new ObjectReferenceKeyframe
                        {
                            time = 0f,
                            value = item.Swap[i]
                        };
                        keyFrames[1] = new ObjectReferenceKeyframe
                        {
                            time = 1 / 60f,
                            value = item.Base[i]
                        };
                        AnimationUtility.SetObjectReferenceCurve(clip, curveBinding, keyFrames);
                    }
                }
                else
                {
                    for (int i = 0; i < item.Swap.Length; i++)
                    {
                        ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[2];

                        EditorCurveBinding curveBinding = EditorCurveBinding.PPtrCurve(GetGameObjectPath(item.MeshRenderer.gameObject, VRCAvatarDescriptors), typeof(SkinnedMeshRenderer), "m_Materials.Array.data[" + i + "]");
                        keyFrames[0] = new ObjectReferenceKeyframe
                        {
                            time = 0f,
                            value = item.Base[i]
                        };
                        keyFrames[1] = new ObjectReferenceKeyframe
                        {
                            time = 1 / 60f,
                            value = item.Swap[i]
                        };
                        AnimationUtility.SetObjectReferenceCurve(clip, curveBinding, keyFrames);
                    }
                }
            }

            clip.name = objects[index].MeshRenderer.gameObject.name + "Selective " + index;
            AssetDatabase.CreateAsset(clip, "Assets" + "/" + VRCAvatarDescriptors.name + "/Animation/" + clip.name + ".anim");

            string path = ("Assets" + "/" + VRCAvatarDescriptors.name + "/Animation/" + clip.name + ".anim");
            clip = AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip)) as AnimationClip;
            SelectorAddClip(clip, index);

        }

        {
            AnimationClip clipoff = new AnimationClip();
            foreach (MaterialSelector item in objects)
            {
                for (int i = 0; i < item.Swap.Length; i++)
                {
                    ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[2];
                    EditorCurveBinding curveBinding = EditorCurveBinding.PPtrCurve(GetGameObjectPath(item.MeshRenderer.gameObject, VRCAvatarDescriptors), typeof(SkinnedMeshRenderer), "m_Materials.Array.data[" + i + "]");
                    keyFrames[0] = new ObjectReferenceKeyframe
                    {
                        time = 0f,
                        value = item.Base[i]
                    };
                    keyFrames[1] = new ObjectReferenceKeyframe
                    {
                        time = 1 / 60f,
                        value = item.Swap[i]
                    };
                    AnimationUtility.SetObjectReferenceCurve(clipoff, curveBinding, keyFrames);
                    curveBinding = EditorCurveBinding.PPtrCurve(GetGameObjectPath(item.MeshRenderer.gameObject, VRCAvatarDescriptors), typeof(SkinnedMeshRenderer), "m_Materials.Array.data[" + i + "].m_FileID");
                    AnimationUtility.SetObjectReferenceCurve(clipoff, curveBinding, keyFrames);
                    curveBinding = EditorCurveBinding.PPtrCurve(GetGameObjectPath(item.MeshRenderer.gameObject, VRCAvatarDescriptors), typeof(SkinnedMeshRenderer), "m_Materials.Array.data[" + i + "].m_PathID");
                    AnimationUtility.SetObjectReferenceCurve(clipoff, curveBinding, keyFrames);
                }
            }
            clipoff.name = "All Swap index " + (objects.Count + 2);
            AssetDatabase.CreateAsset(clipoff, "Assets" + "/" + VRCAvatarDescriptors.name + "/Animation/" + clipoff.name + ".anim");

            string path = ("Assets" + "/" + VRCAvatarDescriptors.name + "/Animation/" + clipoff.name + ".anim");
            clipoff = AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip)) as AnimationClip;
            SelectorAddClip(clipoff, objects.Count + 2);
        }
        {
            AnimationClip clipoff = new AnimationClip();
            foreach (MaterialSelector item in objects)
            {
                for (int i = 0; i < item.Swap.Length; i++)
                {
                    ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[2];
                    EditorCurveBinding curveBinding = EditorCurveBinding.PPtrCurve(GetGameObjectPath(item.MeshRenderer.gameObject, VRCAvatarDescriptors), typeof(SkinnedMeshRenderer), "m_Materials.Array.data[" + i + "]");
                    keyFrames[0] = new ObjectReferenceKeyframe
                    {
                        time = 0f,
                        value = item.Swap[i]
                    };
                    keyFrames[1] = new ObjectReferenceKeyframe
                    {
                        time = 1 / 60f,
                        value = item.Base[i]
                    };
                    AnimationUtility.SetObjectReferenceCurve(clipoff, curveBinding, keyFrames);
                    curveBinding = EditorCurveBinding.PPtrCurve(GetGameObjectPath(item.MeshRenderer.gameObject, VRCAvatarDescriptors), typeof(SkinnedMeshRenderer), "m_Materials.Array.data[" + i + "].m_FileID");
                    AnimationUtility.SetObjectReferenceCurve(clipoff, curveBinding, keyFrames);
                    curveBinding = EditorCurveBinding.PPtrCurve(GetGameObjectPath(item.MeshRenderer.gameObject, VRCAvatarDescriptors), typeof(SkinnedMeshRenderer), "m_Materials.Array.data[" + i + "].m_PathID");
                    AnimationUtility.SetObjectReferenceCurve(clipoff, curveBinding, keyFrames);
                }
            }
            clipoff.name = "All Base index " + (objects.Count + 3);
            AssetDatabase.CreateAsset(clipoff, "Assets" + "/" + VRCAvatarDescriptors.name + "/Animation/" + clipoff.name + ".anim");

            string path = ("Assets" + "/" + VRCAvatarDescriptors.name + "/Animation/" + clipoff.name + ".anim");
            clipoff = AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip)) as AnimationClip;
            SelectorAddClip(clipoff, objects.Count + 3);
        }

    }




    public void SelectorAddClip(AnimationClip clip, int index)
    {
        var State = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddState("Local index " + index);
        var FriendState = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddState("Friend index " + index);
        var GlobalState = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddState("Global index " + index);

        State.motion = clip;
        FriendState.motion = clip;
        GlobalState.motion = clip;

        var AnyToLocalToggleOFFA = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(State);

        AnyToLocalToggleOFFA.AddCondition(AnimatorConditionMode.If, 0, "IsLocal");
        AnyToLocalToggleOFFA.AddCondition(AnimatorConditionMode.If, 0, "Local Allowed");
        AnyToLocalToggleOFFA.AddCondition(AnimatorConditionMode.If, 0, "Local Toggle");
        AnyToLocalToggleOFFA.AddCondition(AnimatorConditionMode.Equals, index, animatorController.parameters[animatorController.parameters.Length - 1].name);
        AnyToLocalToggleOFFA.duration = 0.1f;
        AnyToLocalToggleOFFA.canTransitionToSelf = false;

        var AnyToFriendToggleONA = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(FriendState);
        var AnyToFriendToggleONB = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(FriendState);

        AnyToFriendToggleONA.AddCondition(AnimatorConditionMode.If, 0, "IsOnFriendsList");
        AnyToFriendToggleONA.AddCondition(AnimatorConditionMode.If, 0, "IsUserAllowed");
        AnyToFriendToggleONA.AddCondition(AnimatorConditionMode.If, 0, "Friend Toggle");
        AnyToFriendToggleONA.AddCondition(AnimatorConditionMode.Equals, index, animatorController.parameters[animatorController.parameters.Length - 1].name);
        AnyToFriendToggleONA.duration = 0.1f;
        AnyToFriendToggleONA.canTransitionToSelf = false;

        AnyToFriendToggleONB.AddCondition(AnimatorConditionMode.If, 0, "IsOnFriendsList");
        AnyToFriendToggleONB.AddCondition(AnimatorConditionMode.If, 0, "Friend Allowed");
        AnyToFriendToggleONB.AddCondition(AnimatorConditionMode.If, 0, "Friend Toggle");
        AnyToFriendToggleONB.AddCondition(AnimatorConditionMode.Equals, index, animatorController.parameters[animatorController.parameters.Length - 1].name);
        AnyToFriendToggleONB.duration = 0.1f;
        AnyToFriendToggleONB.canTransitionToSelf = false;

        var AnyToGlobalToggleOFFA = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(GlobalState);
        var AnyToGlobalToggleOFFB = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(GlobalState);

        AnyToGlobalToggleOFFA.AddCondition(AnimatorConditionMode.IfNot, 0, "IsLocal");
        AnyToGlobalToggleOFFA.AddCondition(AnimatorConditionMode.IfNot, 0, "IsOnFriendsList");
        AnyToGlobalToggleOFFA.AddCondition(AnimatorConditionMode.If, 0, "IsUserAllowed");
        AnyToGlobalToggleOFFA.AddCondition(AnimatorConditionMode.If, 0, "Global Toggle");
        AnyToGlobalToggleOFFA.AddCondition(AnimatorConditionMode.Equals, index, animatorController.parameters[animatorController.parameters.Length - 1].name);
        AnyToGlobalToggleOFFA.duration = 0.1f;
        AnyToGlobalToggleOFFA.canTransitionToSelf = false;

        AnyToGlobalToggleOFFB.AddCondition(AnimatorConditionMode.IfNot, 0, "IsLocal");
        AnyToGlobalToggleOFFB.AddCondition(AnimatorConditionMode.IfNot, 0, "IsOnFriendsList");
        AnyToGlobalToggleOFFB.AddCondition(AnimatorConditionMode.If, 0, "Global Allowed");
        AnyToGlobalToggleOFFB.AddCondition(AnimatorConditionMode.If, 0, "Global Toggle");
        AnyToGlobalToggleOFFB.AddCondition(AnimatorConditionMode.Equals, index, animatorController.parameters[animatorController.parameters.Length - 1].name);
        AnyToGlobalToggleOFFB.duration = 0.1f;
        AnyToGlobalToggleOFFB.canTransitionToSelf = false;

    }

    public void SeletorDisplaySetup(AnimationClip baseclip, AnimationClip DisableModel)
    {
        //Disable setup
        //
        //

        var DisableModelState = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddState("Disable Model");

        var AnyToDisableA = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(DisableModelState);
        var AnyToDisableB = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(DisableModelState);
        var AnyToDisableC = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(DisableModelState);

        DisableModelState.motion = DisableModel;

        AnyToDisableA.AddCondition(AnimatorConditionMode.If, 0, "IsLocal");
        AnyToDisableA.AddCondition(AnimatorConditionMode.IfNot, 0, "Local Allowed");
        AnyToDisableA.duration = 0.1f;
        AnyToDisableA.canTransitionToSelf = false;

        AnyToDisableB.AddCondition(AnimatorConditionMode.IfNot, 0, "IsLocal");
        AnyToDisableB.AddCondition(AnimatorConditionMode.IfNot, 0, "IsOnFriendsList");
        AnyToDisableB.AddCondition(AnimatorConditionMode.IfNot, 0, "IsUserAllowed");
        AnyToDisableB.AddCondition(AnimatorConditionMode.IfNot, 0, "Global Allowed");
        AnyToDisableB.duration = 0.1f;
        AnyToDisableB.canTransitionToSelf = false;

        AnyToDisableC.AddCondition(AnimatorConditionMode.If, 0, "IsOnFriendsList");
        AnyToDisableC.AddCondition(AnimatorConditionMode.IfNot, 0, "IsUserAllowed");
        AnyToDisableC.AddCondition(AnimatorConditionMode.IfNot, 0, "Friend Allowed");
        AnyToDisableC.duration = 0.1f;
        AnyToDisableC.canTransitionToSelf = false;


        //State setup
        //
        //

        var GlobalDisplayON = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddState("Global Display ON");
        var FriendDisplayON = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddState("Friend Display ON");
        var LocalDisplayON = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddState("Local Display ON");

        GlobalDisplayON.motion = baseclip;
        FriendDisplayON.motion = baseclip;
        LocalDisplayON.motion = baseclip;

        //Display
        //
        //

        var AnyToGlobalA = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(GlobalDisplayON);
        var AnyToGlobalB = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(GlobalDisplayON);

        AnyToGlobalA.AddCondition(AnimatorConditionMode.IfNot, 0, "IsLocal");
        AnyToGlobalA.AddCondition(AnimatorConditionMode.IfNot, 0, "IsOnFriendsList");
        AnyToGlobalA.AddCondition(AnimatorConditionMode.If, 0, "IsUserAllowed");
        AnyToGlobalA.AddCondition(AnimatorConditionMode.IfNot, 0, "Global Toggle");
        AnyToGlobalA.duration = 0.1f;
        AnyToGlobalA.canTransitionToSelf = false;

        AnyToGlobalB.AddCondition(AnimatorConditionMode.IfNot, 0, "IsLocal");
        AnyToGlobalB.AddCondition(AnimatorConditionMode.IfNot, 0, "IsOnFriendsList");
        AnyToGlobalB.AddCondition(AnimatorConditionMode.If, 0, "Global Allowed");
        AnyToGlobalB.AddCondition(AnimatorConditionMode.IfNot, 0, "Global Toggle");
        AnyToGlobalB.duration = 0.1f;
        AnyToGlobalB.canTransitionToSelf = false;

        var AnyToLocalA = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(LocalDisplayON);

        AnyToLocalA.AddCondition(AnimatorConditionMode.If, 0, "IsLocal");
        AnyToLocalA.AddCondition(AnimatorConditionMode.If, 0, "Local Allowed");
        AnyToLocalA.AddCondition(AnimatorConditionMode.IfNot, 0, "Local Toggle");
        AnyToLocalA.duration = 0.1f;
        AnyToLocalA.canTransitionToSelf = false;

        var AnyToFriendA = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(FriendDisplayON);
        var AnyToFriendB = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(FriendDisplayON);

        AnyToFriendA.AddCondition(AnimatorConditionMode.If, 0, "IsOnFriendsList");
        AnyToFriendA.AddCondition(AnimatorConditionMode.If, 0, "IsUserAllowed");
        AnyToFriendA.AddCondition(AnimatorConditionMode.IfNot, 0, "Friend Toggle");
        AnyToFriendA.duration = 0.1f;
        AnyToFriendA.canTransitionToSelf = false;

        AnyToFriendB.AddCondition(AnimatorConditionMode.If, 0, "IsOnFriendsList");
        AnyToFriendB.AddCondition(AnimatorConditionMode.If, 0, "Friend Allowed");
        AnyToFriendB.AddCondition(AnimatorConditionMode.IfNot, 0, "Friend Toggle");
        AnyToFriendB.duration = 0.1f;
        AnyToFriendB.canTransitionToSelf = false;

    }

    public void SelectiveAddClip(AnimationClip clip)
    {

        var GlobalToggleON = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddState("Global Toggle ON");
        var FriendToggleON = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddState("Friend Toggle ON");
        var LocalToggleON = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddState("Local Toggle ON");

        var GlobalToggleOFF = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddState("Global Toggle OFF");
        var FriendToggleOFF = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddState("Friend Toggle OFF");
        var LocalToggleOFF = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddState("Local Toggle OFF");

        GlobalToggleON.motion = clip;
        FriendToggleON.motion = clip;
        LocalToggleON.motion = clip;

        GlobalToggleOFF.motion = clip;
        FriendToggleOFF.motion = clip;
        LocalToggleOFF.motion = clip;

        GlobalToggleOFF.speed = -1f;
        FriendToggleOFF.speed = -1f;
        LocalToggleOFF.speed = -1f;


        //Global Display
        //
        //

        var AnyToGlobalToggleONA = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(GlobalToggleON);
        var AnyToGlobalToggleONB = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(GlobalToggleON);

        AnyToGlobalToggleONA.AddCondition(AnimatorConditionMode.IfNot, 0, "IsLocal");
        AnyToGlobalToggleONA.AddCondition(AnimatorConditionMode.IfNot, 0, "IsOnFriendsList");
        AnyToGlobalToggleONA.AddCondition(AnimatorConditionMode.If, 0, "IsUserAllowed");
        AnyToGlobalToggleONA.AddCondition(AnimatorConditionMode.If, 0, "Global Toggle");
        AnyToGlobalToggleONA.AddCondition(AnimatorConditionMode.IfNot, 0, animatorController.parameters[animatorController.parameters.Length - 1].name);
        AnyToGlobalToggleONA.duration = 0.1f;
        AnyToGlobalToggleONA.canTransitionToSelf = false;

        AnyToGlobalToggleONB.AddCondition(AnimatorConditionMode.IfNot, 0, "IsLocal");
        AnyToGlobalToggleONB.AddCondition(AnimatorConditionMode.IfNot, 0, "IsOnFriendsList");
        AnyToGlobalToggleONB.AddCondition(AnimatorConditionMode.If, 0, "Global Allowed");
        AnyToGlobalToggleONB.AddCondition(AnimatorConditionMode.If, 0, "Global Toggle");
        AnyToGlobalToggleONB.AddCondition(AnimatorConditionMode.IfNot, 0, animatorController.parameters[animatorController.parameters.Length - 1].name);
        AnyToGlobalToggleONB.duration = 0.1f;
        AnyToGlobalToggleONB.canTransitionToSelf = false;


        var AnyToGlobalToggleOFFA = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(GlobalToggleOFF);
        var AnyToGlobalToggleOFFB = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(GlobalToggleOFF);

        AnyToGlobalToggleOFFA.AddCondition(AnimatorConditionMode.IfNot, 0, "IsLocal");
        AnyToGlobalToggleOFFA.AddCondition(AnimatorConditionMode.IfNot, 0, "IsOnFriendsList");
        AnyToGlobalToggleOFFA.AddCondition(AnimatorConditionMode.If, 0, "IsUserAllowed");
        AnyToGlobalToggleOFFA.AddCondition(AnimatorConditionMode.If, 0, "Global Toggle");
        AnyToGlobalToggleOFFA.AddCondition(AnimatorConditionMode.If, 0, animatorController.parameters[animatorController.parameters.Length - 1].name);
        AnyToGlobalToggleOFFA.duration = 0.1f;
        AnyToGlobalToggleOFFA.canTransitionToSelf = false;

        AnyToGlobalToggleOFFB.AddCondition(AnimatorConditionMode.IfNot, 0, "IsLocal");
        AnyToGlobalToggleOFFB.AddCondition(AnimatorConditionMode.IfNot, 0, "IsOnFriendsList");
        AnyToGlobalToggleOFFB.AddCondition(AnimatorConditionMode.If, 0, "Global Allowed");
        AnyToGlobalToggleOFFB.AddCondition(AnimatorConditionMode.If, 0, "Global Toggle");
        AnyToGlobalToggleOFFB.AddCondition(AnimatorConditionMode.If, 0, animatorController.parameters[animatorController.parameters.Length - 1].name);
        AnyToGlobalToggleOFFB.duration = 0.1f;
        AnyToGlobalToggleOFFB.canTransitionToSelf = false;


        //Friend Display
        //
        //


        var AnyToFriendToggleONA = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(FriendToggleON);
        var AnyToFriendToggleONB = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(FriendToggleON);

        AnyToFriendToggleONA.AddCondition(AnimatorConditionMode.If, 0, "IsOnFriendsList");
        AnyToFriendToggleONA.AddCondition(AnimatorConditionMode.If, 0, "IsUserAllowed");
        AnyToFriendToggleONA.AddCondition(AnimatorConditionMode.If, 0, "Friend Toggle");
        AnyToFriendToggleONA.AddCondition(AnimatorConditionMode.IfNot, 0, animatorController.parameters[animatorController.parameters.Length - 1].name);
        AnyToFriendToggleONA.duration = 0.1f;
        AnyToFriendToggleONA.canTransitionToSelf = false;

        AnyToFriendToggleONB.AddCondition(AnimatorConditionMode.If, 0, "IsOnFriendsList");
        AnyToFriendToggleONB.AddCondition(AnimatorConditionMode.If, 0, "Friend Allowed");
        AnyToFriendToggleONB.AddCondition(AnimatorConditionMode.If, 0, "Friend Toggle");
        AnyToFriendToggleONB.AddCondition(AnimatorConditionMode.IfNot, 0, animatorController.parameters[animatorController.parameters.Length - 1].name);
        AnyToFriendToggleONB.duration = 0.1f;
        AnyToFriendToggleONB.canTransitionToSelf = false;


        var AnyToFriendToggleOFFA = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(FriendToggleOFF);
        var AnyToFriendToggleOFFB = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(FriendToggleOFF);

        AnyToFriendToggleOFFA.AddCondition(AnimatorConditionMode.If, 0, "IsOnFriendsList");
        AnyToFriendToggleOFFA.AddCondition(AnimatorConditionMode.If, 0, "IsUserAllowed");
        AnyToFriendToggleOFFA.AddCondition(AnimatorConditionMode.If, 0, "Friend Toggle");
        AnyToFriendToggleOFFA.AddCondition(AnimatorConditionMode.If, 0, animatorController.parameters[animatorController.parameters.Length - 1].name);
        AnyToFriendToggleOFFA.duration = 0.1f;
        AnyToFriendToggleOFFA.canTransitionToSelf = false;

        AnyToFriendToggleOFFB.AddCondition(AnimatorConditionMode.If, 0, "IsOnFriendsList");
        AnyToFriendToggleOFFB.AddCondition(AnimatorConditionMode.If, 0, "Friend Allowed");
        AnyToFriendToggleOFFB.AddCondition(AnimatorConditionMode.If, 0, "Friend Toggle");
        AnyToFriendToggleOFFB.AddCondition(AnimatorConditionMode.If, 0, animatorController.parameters[animatorController.parameters.Length - 1].name);
        AnyToFriendToggleOFFB.duration = 0.1f;
        AnyToFriendToggleOFFB.canTransitionToSelf = false;


        //Local Display
        //
        //

        var AnyToLocalToggleONA = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(LocalToggleON);

        AnyToLocalToggleONA.AddCondition(AnimatorConditionMode.If, 0, "IsLocal");
        AnyToLocalToggleONA.AddCondition(AnimatorConditionMode.If, 0, "Local Allowed");
        AnyToLocalToggleONA.AddCondition(AnimatorConditionMode.If, 0, "Local Toggle");
        AnyToLocalToggleONA.AddCondition(AnimatorConditionMode.IfNot, 0, animatorController.parameters[animatorController.parameters.Length - 1].name);
        AnyToLocalToggleONA.duration = 0.1f;
        AnyToLocalToggleONA.canTransitionToSelf = false;


        var AnyToLocalToggleOFFA = animatorController.layers[animatorController.layers.Length - 1].stateMachine.AddAnyStateTransition(LocalToggleOFF);

        AnyToLocalToggleOFFA.AddCondition(AnimatorConditionMode.If, 0, "IsLocal");
        AnyToLocalToggleOFFA.AddCondition(AnimatorConditionMode.If, 0, "Local Allowed");
        AnyToLocalToggleOFFA.AddCondition(AnimatorConditionMode.If, 0, "Local Toggle");
        AnyToLocalToggleOFFA.AddCondition(AnimatorConditionMode.If, 0, animatorController.parameters[animatorController.parameters.Length - 1].name);
        AnyToLocalToggleOFFA.duration = 0.1f;
        AnyToLocalToggleOFFA.canTransitionToSelf = false;
    }


    public static string GetGameObjectPath(GameObject obj, GameObject Parent)
    {
        string path = "/" + obj.name;
        while (obj.transform.parent != Parent.transform)
        {
            obj = obj.transform.parent.gameObject;
            path = "/" + obj.name + path;
        }
        return path.Substring(1);
    }

    public void AddGameObject()
    {
        bool YavotDisplay = false;//EditorUtility.DisplayDialog("YAVOT", "Do you want the added toggle to use YAVOT Custom GUI", "Yes", "No");
        foreach (var gameObject in VRCAvatarDescriptors.transform.GetComponentsInChildren<MeshRenderer>())
        {
            serializedObject.Update();
            float[] floats = heights.ToArray();
            Array.Resize(ref floats, stringsProperty.arraySize);
            heights = floats.ToList();
            var index = ro_list.serializedProperty.arraySize;
            ro_list.serializedProperty.arraySize++;
            ro_list.index = index;
            var element = ro_list.serializedProperty.GetArrayElementAtIndex(index);

            element.FindPropertyRelative("Type").enumValueIndex = 0;

            element = element.FindPropertyRelative("MultiToggle");

            element.FindPropertyRelative("ToggleName").stringValue = gameObject.gameObject.name;
            if (YavotDisplay) element.FindPropertyRelative("YAVOTCustomGUIName").stringValue = gameObject.gameObject.transform.parent.name;
            else element.FindPropertyRelative("YAVOTCustomGUIName").stringValue = "";
            element.FindPropertyRelative("IsASelector").boolValue = false;
            element.FindPropertyRelative("UseYAVOTCustomGUI").boolValue = YavotDisplay;
            element.FindPropertyRelative("Prefab").ClearArray();
            element.FindPropertyRelative("Prefab").InsertArrayElementAtIndex(0);
            element.FindPropertyRelative("Prefab").GetArrayElementAtIndex(0).objectReferenceValue = gameObject.gameObject;

            //li.serializedProperty.arraySize++;
            serializedObject.ApplyModifiedProperties();
        }
        foreach (var gameObject in VRCAvatarDescriptors.transform.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            serializedObject.Update();
            float[] floats = heights.ToArray();
            Array.Resize(ref floats, stringsProperty.arraySize);
            heights = floats.ToList();
            var index = ro_list.serializedProperty.arraySize;
            ro_list.serializedProperty.arraySize++;
            ro_list.index = index;
            var element = ro_list.serializedProperty.GetArrayElementAtIndex(index);

            element.FindPropertyRelative("Type").enumValueIndex = 0;

            element = element.FindPropertyRelative("MultiToggle");

            element.FindPropertyRelative("ToggleName").stringValue = gameObject.gameObject.name;
            if (YavotDisplay) element.FindPropertyRelative("YAVOTCustomGUIName").stringValue = gameObject.gameObject.transform.parent.name;
            else element.FindPropertyRelative("YAVOTCustomGUIName").stringValue = "";
            element.FindPropertyRelative("IsASelector").boolValue = false;
            element.FindPropertyRelative("UseYAVOTCustomGUI").boolValue = YavotDisplay;
            element.FindPropertyRelative("Prefab").ClearArray();
            element.FindPropertyRelative("Prefab").InsertArrayElementAtIndex(0);
            element.FindPropertyRelative("Prefab").GetArrayElementAtIndex(0).objectReferenceValue = gameObject.gameObject;

            //li.serializedProperty.arraySize++;
            serializedObject.ApplyModifiedProperties();
        }
    }







    [MenuItem("Cat/AnimatorController")]
    public static void ShowWindow()
    {
        GetWindow<AnimatorCreator>("Cat's Animator Creator");
    }
    Vector2 scrollPosition = Vector2.zero;
    void OnGUI()
    {
        try
        {
            if (VRCAvatarDescriptors == null)
            {
                try
                {
                    VRCAvatarDescriptors = ((VRCAvatarDescriptor)EditorGUILayout.ObjectField("VRChat Avatar", VRCAvatarDescriptors, typeof(VRCAvatarDescriptor), true)).gameObject;
                }
                catch (Exception) { }
                return;
            }
            else
            {
                //AnimLayerType.FX
                VRCAvatarDescriptors = ((VRCAvatarDescriptor)EditorGUILayout.ObjectField("VRChat Avatar", VRCAvatarDescriptors.GetComponent<VRCAvatarDescriptor>(), typeof(VRCAvatarDescriptor), true)).gameObject;
            }
        }
        catch { VRCAvatarDescriptors = null; return; }
        SelectiveAviDisplay = EditorGUILayout.Toggle(new GUIContent("Selective avi Display", "will allow selective display for friend, global, local"), SelectiveAviDisplay);
        if (SelectiveAviDisplay) ModelHidden = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Model Hidden Display", "This, and their children, will display when the model is hidden"), ModelHidden, typeof(GameObject), true);
        animatorController = (AnimatorController)EditorGUILayout.ObjectField("FX Layer", animatorController, typeof(AnimatorController), true);
        var btn1 = GUILayout.Button("Add GameObject Automatically");
        if (animatorController != null && btn1) AddGameObject();
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false);
        if (this.serializedObject != null)
        {
            serializedObject.Update();
            ro_list.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }
        GUILayout.EndScrollView();
        GUILayout.FlexibleSpace();
        var btn = GUILayout.Button("Create Toggle");
        if (animatorController != null && btn) CreateController();

    }
    static ReorderableList ro_list;
    SerializedObject serializedObject;
    static SerializedProperty stringsProperty;
    static MyScriptableObject obj;
    public List<float> heights;

    private void OnEnable()
    {
        obj = ScriptableObject.CreateInstance<MyScriptableObject>();

        serializedObject = new UnityEditor.SerializedObject(obj);
        stringsProperty = serializedObject.FindProperty("ToggleTyped");
        heights = new List<float>(stringsProperty.arraySize);
        ro_list = new ReorderableList(serializedObject, stringsProperty, true, true, true, true);

        ro_list.drawElementCallback = (rect, index, active, focused) =>
        {
            SerializedProperty element = ro_list.serializedProperty.GetArrayElementAtIndex(index);
            float Spacing = EditorGUIUtility.singleLineHeight + 5;
            EditorGUI.PropertyField(new Rect(rect.x + rect.width - 180, rect.y, 180, EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("Type"), GUIContent.none, true);
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), element.FindPropertyRelative(ro_list.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Type").enumNames[ro_list.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Type").enumValueIndex]), new GUIContent(element.FindPropertyRelative(ro_list.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Type").enumNames[ro_list.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Type").enumValueIndex]).FindPropertyRelative("ToggleName").stringValue), true);
            bool foldout = element.FindPropertyRelative(ro_list.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Type").enumNames[ro_list.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Type").enumValueIndex]).isExpanded;
            foreach (SerializedProperty ev in element.FindPropertyRelative("MaterialSwap").FindPropertyRelative("MaterialSelector"))
            {
                if (ev.FindPropertyRelative("MeshRenderer").objectReferenceValue != null)
                {
                    ev.FindPropertyRelative("Base").arraySize = ((SkinnedMeshRenderer)ev.FindPropertyRelative("MeshRenderer").objectReferenceValue).sharedMaterials.Length;
                    for (int mat = 0; mat < ((SkinnedMeshRenderer)ev.FindPropertyRelative("MeshRenderer").objectReferenceValue).sharedMaterials.Length; mat++)
                    {
                        ev.FindPropertyRelative("Base").GetArrayElementAtIndex(mat).objectReferenceValue = ((SkinnedMeshRenderer)ev.FindPropertyRelative("MeshRenderer").objectReferenceValue).sharedMaterials[mat]; ;
                    }

                    if (ev.FindPropertyRelative("Base").arraySize != ev.FindPropertyRelative("Swap").arraySize)
                    {
                        ev.FindPropertyRelative("Swap").arraySize = ev.FindPropertyRelative("Base").arraySize;
                        for (int mat = 0; mat < ((SkinnedMeshRenderer)ev.FindPropertyRelative("MeshRenderer").objectReferenceValue).sharedMaterials.Length; mat++)
                        {
                            ev.FindPropertyRelative("Swap").GetArrayElementAtIndex(mat).objectReferenceValue = ((SkinnedMeshRenderer)ev.FindPropertyRelative("MeshRenderer").objectReferenceValue).sharedMaterials[mat];
                        }
                    }
                }
            }

            //height Adjustment thingy
            {
                float height = EditorGUIUtility.singleLineHeight;
                if (foldout)
                {
                    height = (EditorGUIUtility.singleLineHeight) * 6 + 10;
                    if (ro_list.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Type").enumNames[ro_list.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Type").enumValueIndex] == "MultiToggle")
                    {
                        if (element.FindPropertyRelative("MultiToggle").FindPropertyRelative("Prefab").isExpanded)
                        {
                            height = height + (EditorGUIUtility.singleLineHeight) * 2 + ((EditorGUIUtility.singleLineHeight + 2) * Math.Clamp(element.FindPropertyRelative("MultiToggle").FindPropertyRelative("Prefab").arraySize - 1, 0, int.MaxValue)) + 20;
                        }
                    }
                    if (ro_list.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Type").enumNames[ro_list.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Type").enumValueIndex] == "MaterialSwap")
                    {
                        if (element.FindPropertyRelative("MaterialSwap").FindPropertyRelative("MaterialSelector").isExpanded)
                        {
                            height = height + (EditorGUIUtility.singleLineHeight) * 2 + ((EditorGUIUtility.singleLineHeight + 2) * Math.Clamp(element.FindPropertyRelative("MaterialSwap").FindPropertyRelative("MaterialSelector").arraySize, 0, int.MaxValue));
                            foreach (SerializedProperty ev in element.FindPropertyRelative("MaterialSwap").FindPropertyRelative("MaterialSelector"))
                            {

                                if (ev.isExpanded)
                                {
                                    height = height + (EditorGUIUtility.singleLineHeight) * 2 + ((EditorGUIUtility.singleLineHeight + 5) * Math.Clamp(element.FindPropertyRelative("MaterialSwap").FindPropertyRelative("MaterialSelector").arraySize, 0, int.MaxValue));
                                }
                                else
                                {
                                    ev.FindPropertyRelative("Base").isExpanded = false;
                                    ev.FindPropertyRelative("Swap").isExpanded = false;
                                }
                                if (ev.FindPropertyRelative("Base").isExpanded)
                                {
                                    height = height + (EditorGUIUtility.singleLineHeight) * 2 + ((EditorGUIUtility.singleLineHeight + 2) * Math.Clamp(ev.FindPropertyRelative("Base").arraySize - 1, 0, int.MaxValue));
                                }
                                if (ev.FindPropertyRelative("Swap").isExpanded)
                                {
                                    height = height + (EditorGUIUtility.singleLineHeight) * 2 + ((EditorGUIUtility.singleLineHeight + 2) * Math.Clamp(ev.FindPropertyRelative("Swap").arraySize - 1, 0, int.MaxValue));
                                }
                            }
                        }
                        else
                        {
                            foreach (SerializedProperty ev in element.FindPropertyRelative("MaterialSwap").FindPropertyRelative("MaterialSelector"))
                            {
                                ev.isExpanded = false;
                                ev.FindPropertyRelative("Base").isExpanded = false;
                                ev.FindPropertyRelative("Swap").isExpanded = false;
                            }
                        }
                    }
                }


                try
                {
                    heights[index] = height;
                }
                catch (ArgumentOutOfRangeException e)
                {
                    Debug.LogWarning(e.Message);
                }
                finally
                {
                    float[] floats = heights.ToArray();
                    Array.Resize(ref floats, stringsProperty.arraySize);
                    heights = floats.ToList();
                }

                float margin = height / 10;
                rect.y += margin;
                rect.height = (height / 5) * 4;
                rect.width = rect.width / 2 - margin / 2;
                rect.x += rect.width + margin;
            }
        };

        ro_list.elementHeightCallback = (index) =>
        {
            Repaint();
            float height = 0;

            try
            {
                height = heights[index];
            }
            catch (ArgumentOutOfRangeException e)
            {
                Debug.LogWarning(e.Message);
            }
            finally
            {
                float[] floats = heights.ToArray();
                Array.Resize(ref floats, stringsProperty.arraySize);
                heights = floats.ToList();
            }

            return height;
        };
        ro_list.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(new Rect(rect.width / 2, rect.y, rect.width, rect.height), "Toggles List");
        };
        ro_list.onAddDropdownCallback = (rect, li) =>
        {
            serializedObject.Update();
            float[] floats = heights.ToArray();
            Array.Resize(ref floats, stringsProperty.arraySize);
            heights = floats.ToList();
            var index = ro_list.serializedProperty.arraySize;
            ro_list.serializedProperty.arraySize++;
            ro_list.index = index;
            var element = ro_list.serializedProperty.GetArrayElementAtIndex(index);

            /*
            element.FindPropertyRelative("ToggleName").stringValue = "";
            element.FindPropertyRelative("YAVOTCustomGUIName").stringValue = "";
            element.FindPropertyRelative("UseYAVOTCustomGUI").boolValue = false;
            element.FindPropertyRelative("Prefab").objectReferenceValue = null;
            */
            element.FindPropertyRelative("Type").enumValueIndex = 0;

            element = element.FindPropertyRelative("MultiToggle");

            element.FindPropertyRelative("ToggleName").stringValue = "";
            element.FindPropertyRelative("YAVOTCustomGUIName").stringValue = "";
            element.FindPropertyRelative("UseYAVOTCustomGUI").boolValue = false;
            element.FindPropertyRelative("IsASelector").boolValue = false;
            element.FindPropertyRelative("Prefab").ClearArray();

            element = ro_list.serializedProperty.GetArrayElementAtIndex(index);

            element = element.FindPropertyRelative("MaterialSwap");

            element.FindPropertyRelative("ToggleName").stringValue = "";
            element.FindPropertyRelative("YAVOTCustomGUIName").stringValue = "";
            element.FindPropertyRelative("UseYAVOTCustomGUI").boolValue = false;
            foreach (SerializedProperty ev in element.FindPropertyRelative("MaterialSelector"))
            {
                ev.FindPropertyRelative("MeshRenderer").objectReferenceValue = null;
                ev.FindPropertyRelative("Base").ClearArray();
                ev.FindPropertyRelative("Swap").ClearArray();
            }
            element.FindPropertyRelative("MaterialSelector").ClearArray();

            //ro_list.serializedProperty.arraySize++;
            serializedObject.ApplyModifiedProperties();

        };
    }
}

#endif
