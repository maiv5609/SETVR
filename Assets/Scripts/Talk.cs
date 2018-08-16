using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Diagnostics;
using UnityEngine.UI;
using MORPH3D;
using MORPH3D.FOUNDATIONS;

public class Talk : MonoBehaviour {

    private bool talk = false;
    private bool open = false;
    private M3DCharacterManager m_CharacterManager;

    // Use this for initialization
    void Start()
    {
        m_CharacterManager = GetComponent<M3DCharacterManager>();
        StartCoroutine(moveMouth());
        m_CharacterManager.SetBlendshapeValue("eCTRLvER", 0);
    }

    public void startTalking()
    {
        InvokeRepeating("mouthOpen", 0.0f, 0.8f);
    }

    public void stopTalking()
    {
        m_CharacterManager.SetBlendshapeValue("eCTRLvER", 0);
        CancelInvoke();
    }

    public void mouthOpen()
    {
        //Open mouth and invoke close mouth 0.3 sec
        m_CharacterManager.SetBlendshapeValue("eCTRLvER", 100);
        Invoke("mouthClose", 0.3f);
    }

    public void mouthClose()
    {
        m_CharacterManager.SetBlendshapeValue("eCTRLvER", 0);
    }
}
