using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "new Player Settings",menuName ="Player/Player Settings")]
public class PlayerSettingsSO : ScriptableObject
{
    public float cameraSensitivity = 0f;
    public float playerSpeed = 0f;
    public float jumpHeight = 2f;
}
