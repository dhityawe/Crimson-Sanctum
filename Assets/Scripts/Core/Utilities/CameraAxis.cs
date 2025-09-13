using Unity.Cinemachine;
using UnityEngine;

[ExecuteInEditMode]
[SaveDuringPlay]
[AddComponentMenu("")]
public class CameraAxis : CinemachineExtension
{
    [Tooltip("Change this value to fixed the camera x axis")]
    [SerializeField] private float m_XPosition;

    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage == CinemachineCore.Stage.Finalize)
        {
            var pos = state.GetCorrectedPosition();
            pos.x = m_XPosition;
            state.RawPosition = pos;
            state.PositionCorrection = Vector3.zero;
        }
    }
}
