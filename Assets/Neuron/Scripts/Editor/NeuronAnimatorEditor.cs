//
// Custom editor for Neuron animator class
//
// Refactored by Keijiro Takahashi
// https://github.com/keijiro/NeuronRetargeting
//
// This is a derivative work of the Perception Neuron SDK. You can use this
// freely as one of "Perception Neuron SDK Derivatives". See LICENSE.pdf and
// their website for further details.
//

using UnityEngine;
using UnityEditor;

namespace Neuron
{
    [CanEditMultipleObjects, CustomEditor(typeof(NeuronAnimator))]
    public class NeuronAnimatorEditor : Editor
    {
        SerializedProperty _address;
        SerializedProperty _port;
        SerializedProperty _socketType;
        SerializedProperty _actorID;

        void OnEnable()
        {
            _address = serializedObject.FindProperty("_address");
            _port = serializedObject.FindProperty("_port");
            _socketType = serializedObject.FindProperty("_socketType");
            _actorID = serializedObject.FindProperty("_actorID");
        }

        public override void OnInspectorGUI()
        {
            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "The settings in this component is not editable while " +
                    "playing. Exit the play mode to change the settings.",
                    MessageType.None, true
                );
                return;
            }

            serializedObject.Update();

            EditorGUILayout.PropertyField(_address);
            EditorGUILayout.PropertyField(_port);
            EditorGUILayout.PropertyField(_socketType);
            EditorGUILayout.PropertyField(_actorID);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
