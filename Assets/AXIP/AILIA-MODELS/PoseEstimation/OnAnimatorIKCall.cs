using UnityEngine;
using UnityEngine.Events;

public class OnAnimatorIKCall : MonoBehaviour
{
	[SerializeField]
	public OnAnimatorIKEvent Function = null;
    private void OnAnimatorIK(int layerIndex)
	{
		Function?.Invoke(layerIndex);
	}
}

[System.Serializable]
public class OnAnimatorIKEvent : UnityEvent<int>
{
}