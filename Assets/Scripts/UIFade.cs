using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIFade : MonoBehaviour {
	
	public CanvasGroup uiElement;

	
	public void FadeIn(){
		StartCoroutine(FadeCanvasGroup(uiElement, uiElement.alpha, 1));
	}
	
	public void FadeOut(){
		StartCoroutine(FadeCanvasGroup(uiElement, uiElement.alpha, 0));
	}
	
	public IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float lerpTime = 0.5f){
		//Lerp = Linearly interpolating
		float timeStartedLerping = Time.time;
		float timeSinceStarted = Time.time - timeStartedLerping;
		float percentageComplete = timeSinceStarted / lerpTime;
		float currentValue= 0.0f;
		
		while(percentageComplete <= 1){
			timeSinceStarted = Time.time - timeStartedLerping;
			percentageComplete = timeSinceStarted / lerpTime;
			currentValue = Mathf.Lerp(start, end, percentageComplete);
			
			cg.alpha = currentValue;
			
			yield return new WaitForEndOfFrame();
		}
	}
}
