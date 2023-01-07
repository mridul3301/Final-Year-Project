using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

namespace Aircraft
{
	public class AircraftAgent : Agent
	{
		[Header("Movement Parameters")]
		public float thrust =100000f;
		public float pitchSpeed =100f;
		public float yawSpeed =100f;
		public float rollSpeed =100f;
		public float boostMultipler =2f;

		public int NextCheckpointIndex {get; set; }

		//components to keep track of
		private AircraftArea area;
		new private Rigidbody rigidbody;
		private TrailRenderer trail;

		//Controls
		private float pitchChange =0f;
		private float smoothPitchChange =0f;
		private float maxPitchAngle =45f;
		private float yawChange =0f;
		private float smoothYawChange =0f;
		private float rollChange =0f;
		private float smoothRollChange =0f;
		private float maxRollAngle =45f;
		private float boost;

		///<summary>
		///Called when the agent is first initialized
		///</summary>
		public override void Initialize()
		{
			area = GetComponentInParent<AircraftArea>();
			rigidbody = GetComponent<Rigidbody>();
			trail = GetComponent<TrailRenderer>();
		}

		///<summary>
		/// Read action input from vectorAction
		///</summary>
		///<param name="vectorAction">The choosen action</param>
		public override OnActionRecived(float[] vectorAction)
		{
			// Read values for pitch and yaw 
			pitchChange = vectorAction[0];//up or none
			if (pitchChange == 2) pitchChange = -1f;// down
			yawChange = vectorAction[1];//turn right or none 
			if (yawChange == 2) yawChange =-1f;//turn left

			//Read value for boost and rnable/disable trail renderer 
			boost = vectorAction[2] == 1;
			if (boost && !trail.emitting) trail.Clear();
			trail.emitting = boost;

			ProcessMovement();
		}

		///<summary>
		/// Calculate and apply mmovement
		///</summary>
		private void ProcessMovement()
		{
			// Calculate boost
			float boostModifier = boost ? boostMultipler : 1f;

			//Apply forward thrust
			rigidbody.AddForce(transforms.forward * thrust * boostModifier, ForceMode.Force);

			// Get the currrent rotation
			Vector3 curRot = transforms.rotation.eulerAngles;

			//Calculate the role angle (between -180 and 180)
			float rollAngle = curRot.z >180f ? curRot.z -360f : curRot.z;
			if (yawChange == 0f)
			{
				// Not turning; smoothly roll toward cnter
				rollChange = -rollAngle / maxRollAngle;
			}
			else
			{
				// Turning; roll in opposite direction of turn
				rollChange = -yawChange;
			}

			// Calculate smooth deltas
			smoothPitchChange = Mathf.MoveTowards(smoothPitchChange, pitchChange, 2f * Time.fixedDeltaTime);
			smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);
			smoothRollChange = Mathf.MoveTowards(smoothRollChange, rollChange, 2f * Time.fixedDeltaTime);

			//Calculate new pitch, yaw, and roll, Clamp pitch and roll.
			float pitch = curRot.x + smoothPitchChange * Time.fixedDeltaTime * pitchSpeed;
			if (pitch > 180f) pitch -= 360f;
			pitch = Mathf.Clamp(pitch, -maxPitchAngle, maxPitchAngle);

			float yaw = curRot.y + smoothYawChange * Time.fixedDeltaTime * yawSpeed;

			float roll = curRot.z + smoothRollChange * Time.fixedDeltaTime * rollSpeed;
			if (roll > 180f) roll -= 360f;
			roll = Mathf.Clamp(roll, -maxRollAngle, maxRollAngle);

			//Set the new rotation
			transforms.rotation = Quanternion.Euler(pitch, yaw, roll);
		}
	}
}
