using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HappyEngine.Base
{
    public class CoroutineManager : MonoBehaviour
    {
        private static CoroutineManager _instance;
        public static CoroutineManager Instance => _instance;

        private void Awake()
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[CoroutineManager] Initialized on " + gameObject.name);
        }
    }
}
