using UnityEngine;
// ReSharper disable CommentTypo

namespace RealisticEyeMovements {

	public class BlinkingComponent
	{
		#region fields

			public float blink01 { get; private set; }

			readonly EyeAndHeadAnimator eyeAndHeadAnimator;
				
			float timeTillNextBlink;
			public float timeSinceLastBlinkFinished { get; private set; }

				enum BlinkState {
					Idle,
					Closing,
					Closed,
					EarlyOpeningAccelerating,
					EarlyOpeningDecelerating,
					LateOpening
				}
				BlinkState blinkState = BlinkState.Idle;
				float blinkStateTime;
				float blinkStateDuration;
				float blinkLerpStart;
				float blinkLerpEnd;
				float blinkLerpMaxSpeedClosing;
				float blinkLerpMaxSpeedForEarlyOpening;
				float blinkLerpEndSpeedForEarlyOpening;
				float blinkLerpAccelerationClosing;
				float blinkLerpAccelerationEarlyOpening1;
				float blinkLerpAccelerationLateOpening;
				float blinkLerpSpeed;
				bool isShortBlink;
				
				const float kShortBlinkFactor = 0.8f;
				// From "High-speed camera characterization of voluntary eye blinking kinematics", Kwon 2013
				const float kBlinkCloseDuration = 79.88f * 0.001f;
				const float kBlinkClosedDuration = 22.99f * 0.001f;
				const float kBlinkEarlyOpenAccelerationDuration = 53.43f * 0.001f;
				const float kBlinkEarlyOpenDecelerationDuration = 117.48f * 0.001f;
				const float kBlinkLateOpenDuration = 300f * 0.001f;
				
				const float kBlinkLerpAtEarlyOpenAccelerationEnd = 1- 0.34f;
				const float kBlinkLerpAtEarlyOpenDecelerationEnd = 1-0.84f;

		#endregion

		
		public BlinkingComponent(EyeAndHeadAnimator eyeAndHeadAnimator)
		{
			this.eyeAndHeadAnimator = eyeAndHeadAnimator;
			
			blink01 = 0;
			
			ResetBlinking();
		}
		
		
		public void Blink( bool isShortBlink =true)
		{
			if ( blinkState != BlinkState.Idle )
				return;

			timeSinceLastBlinkFinished = 0;
			this.isShortBlink = isShortBlink;
			blinkState = BlinkState.Closing;
			blinkStateTime = 0;
			blinkLerpStart = 0;
			blinkLerpEnd = 1;
			
			blinkStateDuration = (isShortBlink ? kShortBlinkFactor : 1) * kBlinkCloseDuration;

			blinkLerpMaxSpeedClosing = (blinkLerpEnd -blinkLerpStart)/blinkStateDuration;
			
			float lateOpenDuration = (isShortBlink ? kShortBlinkFactor : 1) * kBlinkLateOpenDuration;
			blinkLerpAccelerationLateOpening = 2 * (0 - kBlinkLerpAtEarlyOpenDecelerationEnd)/(lateOpenDuration*lateOpenDuration);
			blinkLerpEndSpeedForEarlyOpening = blinkLerpAccelerationLateOpening * lateOpenDuration;
			
			blinkLerpAccelerationClosing = 4 * blinkLerpMaxSpeedClosing * blinkLerpMaxSpeedClosing / (blinkLerpEnd -blinkLerpStart);
		}


		public void ResetBlinking()
		{
			timeTillNextBlink = Random.Range(Mathf.Min(eyeAndHeadAnimator.kMinNextBlinkTime, eyeAndHeadAnimator.kMaxNextBlinkTime),
				Mathf.Max(eyeAndHeadAnimator.kMinNextBlinkTime, eyeAndHeadAnimator.kMaxNextBlinkTime));
		}


		public void UpdateBlinking(float deltaTime)
		{
			if ( blinkState == BlinkState.Idle )
			{
				timeSinceLastBlinkFinished += deltaTime;
				timeTillNextBlink -= deltaTime;
				if ( timeTillNextBlink <= 0 )
					Blink();
				
				return;
			}
			
			float modifiedDeltaTime = deltaTime*eyeAndHeadAnimator.blinkSpeed;
			blinkStateTime = Mathf.Min(blinkStateDuration, blinkStateTime + modifiedDeltaTime);
			
			if ( blinkState == BlinkState.Closing )
			{
				if ( blinkStateTime < blinkStateDuration/2 )
					blink01 = Mathf.Clamp01(blinkLerpStart + 0.5f * blinkLerpAccelerationClosing * blinkStateTime * blinkStateTime);
				else
				{
					float timeFromEnd = blinkStateDuration - blinkStateTime;
					blink01 = Mathf.Clamp01(blinkLerpEnd - 0.5f * blinkLerpAccelerationClosing * timeFromEnd * timeFromEnd);
				}

				if ( blinkStateTime >= blinkStateDuration )
				{
					blinkState = BlinkState.Closed;
					blinkStateTime = 0;
					blinkStateDuration = (isShortBlink ? kShortBlinkFactor : 1) * kBlinkClosedDuration;
				}
			}
			if ( blinkState == BlinkState.Closed )
			{
				if ( blinkStateTime >= blinkStateDuration )
				{
					blinkState = BlinkState.EarlyOpeningAccelerating;
					blinkStateTime = 0;
					blinkLerpStart = 1;
					blinkStateDuration = (isShortBlink ? kShortBlinkFactor : 1) * kBlinkEarlyOpenAccelerationDuration;
					blinkLerpAccelerationEarlyOpening1 = 2 * (kBlinkLerpAtEarlyOpenAccelerationEnd - 1) / (blinkStateDuration*blinkStateDuration);
				}
			}
			if ( blinkState == BlinkState.EarlyOpeningAccelerating )
			{
				blink01 = Mathf.Clamp01(blinkLerpStart + 0.5f * blinkLerpAccelerationEarlyOpening1 * blinkStateTime * blinkStateTime);
				
				if ( blink01 <= kBlinkLerpAtEarlyOpenAccelerationEnd )
				{
					blinkState = BlinkState.EarlyOpeningDecelerating;
					blinkStateTime = 0;
					blinkLerpSpeed = blinkLerpMaxSpeedForEarlyOpening = blinkLerpAccelerationEarlyOpening1 * blinkStateDuration;
					blinkStateDuration = (isShortBlink ? kShortBlinkFactor : 1) * kBlinkEarlyOpenDecelerationDuration;
				}
			}
			if ( blinkState == BlinkState.EarlyOpeningDecelerating )
			{
				blink01 = Mathf.Clamp01(blink01 + blinkLerpSpeed * modifiedDeltaTime);
				blinkLerpSpeed = Mathf.Lerp(blinkLerpMaxSpeedForEarlyOpening, blinkLerpEndSpeedForEarlyOpening, Mathf.InverseLerp(kBlinkLerpAtEarlyOpenAccelerationEnd, kBlinkLerpAtEarlyOpenDecelerationEnd, blink01));
				
				if ( blink01 <= kBlinkLerpAtEarlyOpenDecelerationEnd )
				{
					blinkState = BlinkState.LateOpening;
					blinkStateDuration = (isShortBlink ? kShortBlinkFactor : 1) * kBlinkLateOpenDuration;
					float lateOpeningTime = Mathf.Sqrt(2 * blink01/Mathf.Abs(blinkLerpAccelerationLateOpening));
					blinkStateTime = Mathf.Max(0, blinkStateDuration - lateOpeningTime);
				}
			}
			if ( blinkState == BlinkState.LateOpening )
			{
				float timeFromEnd = blinkStateDuration - blinkStateTime;
				blink01 = -0.5f * blinkLerpAccelerationLateOpening * timeFromEnd * timeFromEnd;

				if ( blink01 <= 0 )
				{
					blink01 = 0;
					blinkState = BlinkState.Idle;
					timeSinceLastBlinkFinished = 0;
					
					ResetBlinking();
				}
			}
		}

	}
}