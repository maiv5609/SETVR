using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MORPH3D;
using MORPH3D.FOUNDATIONS;

public class Talk : MonoBehaviour {

    private bool talk = true;
    private bool open = false;
    private M3DCharacterManager m_CharacterManager;

    // Use this for initialization
    void Start()
    {
        m_CharacterManager = GetComponent<M3DCharacterManager>();
        //GetComponent<M3DCharacterManager>().coreMorphs.morphLookup["eCTRLvER"];
        StartCoroutine(moveMouth());
    }

    IEnumerator moveMouth()
    {
        while (talk)
        {
            yield return new WaitForSeconds(0.3f);
            if (!open)
            {
				LeanTween.value (gameObject, 0.0f, 100.0f, 0.3f).setOnUpdate ((float value) => {
					m_CharacterManager.SetBlendshapeValue("eCTRLvER", 100);
				}).setEase (LeanTweenType.easeInCubic);
                open = true;
            }
            else
            {
				LeanTween.value (gameObject, 0.0f, 100.0f, 0.3f).setOnUpdate ((float value) => {
					m_CharacterManager.SetBlendshapeValue("eCTRLvER", 0);
				}).setEase (LeanTweenType.easeInCubic);
                open = false;
            }
        }
    }
}
