// Scott Johnson
// 8/3/2007
// You are free to use this code for whatever you wish.
// I release any copyrights to the public domain for this work. (The attorney at
// my company said I needed to do that to prevent my employer from claiming rights to work I do after hours).
// The purpose is to point the eyes (or neck) at a target point.  This was called
// "head tracking" while I was working on a popular football game but is more
// appropriately called "eye/head aiming".

// Each of these dates represents 1-3 hours (after my kids went to bed).

// Earlier than 7/15/2007.
// Downloaded Visual C# and XNA.  Found the skinning sample and realized it would be a good sandbox.

// 7/15/2007
// Not sure the head is bone 8
// Left off.  Eyes track nicely by themselves except for that blip at the start of the anim loop.
// Next try to use full math of head tracking solution by pointing the eyes using the neck.
// Head transform looks unlike other bones.  May screw up my solution.  My rotations for heading and pitch would be wrong axis and errorSquared and grad would be wrong.

// 7/16/2007
// Head and neck work together to track target point!
// eyes don't seem to look right at me though for some reason. :-(
// Up close, the eyes are wobbling.
// Error from yesterday was that I needed a transformNormal call instead of just transform in the gradient
// He started wobbling his head when I made the same transformNormal fix in the error2 function.
// Seating 2 : the eye wobbling is horrible.  Tried to code it so that it couldn't overshoot the minimum error
// and it had no effect.  Only eyes are on right now while I debug the eye wobble.

// 7/17/2007
// Stupid me.  The joint pitch and heading were not affecting the skeleton unless the error was enough to
// require a correction.  The wobbling is gone.  Doh!
// Still need to write clipping to an ellipse.
// Code with spine3 isn't working.  Perhaps there are more bones involved.
// Doesn't work when he has to track the long way around like when the target point is directly behind him.
// Still doesn't look directly at camera.

// 7/18/2007
// Used new formula using "aim" instead of error to get tracking to work more than 90 degrees off target.
// Found that solution has error in it regarding what is passed to aimFunction and grad.
// With the eyes and neck acting together, they aim without proper regard for the translation of the neck
// to the eyes.  When the target point is at neck level, the aim of the eyes is still straight ahead
// even though they should aim slightly down.  Next time, pay close attention to the space of the targetPoint
// compared to the space of the joint.

// 7/19/2007
// Still not pointing at correct spot.  Target Spot in rotated joint space is wrong.  Still need to pay careful 
// attention to what space the aim calculation needs to be in.
// Success : redid how the system changes the animation.  Modifier matrices now affect the global matrices.
// It removes the problem of when the boneToParent matrices are modified.

// 7/22/2007
// Figured out what spaces the calculation expects and I think I have that correct.
// Also figured out today that my new simplified calculation based on "aim" doesn't work unless I have (targetPoint - p0)
// a unit vector.  A basic vector math mistake really.  Now the gradient is much more complicated.
// Left off with character with only neck joint activated and an exaggerated eyeToNeck translation looking down
// all the time.

// 7/23/2007
// Took a break.  Got subversion running to store my code and Mathematica worksheets.

// 7/24/2007
// Found parenthesis bug in gradient.  
// Unexpected result is that the eyes are always pointed at the target.  This is good, however the neck tends
// to wander.  He sometimes turns his head right while his eyes look left.  His joints work against each other.
// Suppose he looking far to the left and has to use his neck to get there.  Now the target moves to his right.
// He may be able to look forward by keeping his neck in the same position but looking with his eyes to the right.
// People don't do that but it is an acceptable solution to the math.  
// That may be correct for what I've programmed.  I have to think about it some more.
// Eyes aren't looking same place for some reason.

// 7/27/2007
// Removed code which copied the left eye rotation to the right.  Didn't need it any more.
// Added hack heuristic to reduce the neck rotation if the aim is very high.  The idea is that next frame
// the eyes can correct from this little change and the neck will tend to not rotate opposite of the eyes over time.
// Coded the trackWithEyes feature and it works well.  It treats the neck as the effector but still points the eyes.

// 7/28/2007
// Pretty happy with it.  It still won't track the head the long way around which is bad and it still doesn't clip the
// pitch,heading of a joint to an ellipse so his neck in sometimes down when looking all the way to one side.
// Spending time to comment.
// Thought about optimizing the code but as soon as I do that I'll want to play with it some more and the optimized
// code is never maintainable.

// 7/29/2007 ?
// Wrote clipping of pitch/heading to an ellipse.  It helps prevent freakish looking eyes and is pretty appropriate
// for the neck too.

#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
#endregion

namespace BetterSkinned
{
 
    // this depends on the skeleton.  This code is implemented specifically for the SkinningSample.
    public enum BoneEnum
    {
        spine2 = 6,
        spine3 = 7,
        neck = 8,
        head = 9,
        rightEye = 13,
        leftEye = 12,
    }

    /// <summary>
    /// Rotation is all the information necessary to modify the rotation of a joint from what the animation system
    /// says.  The idea is that a chain of these (eye, neck, spine3) will be used to point the effector
    /// (eyes or neck) at a target.  The list of Rotation must account for all the rotations in the chain
    /// from the starting bone to the end effector bone
    /// </summary>
    public class Rotation
    {
        public BoneEnum bone;
        public float pitch;            // radians.  Amount of pitch rotation around X axis used on the bone.
        public float heading;          // radians.  Amount of heading rotation around Z axis used on the bone
        public float headingLimit;     // radians
        public float pitchLimit;       // radians
        public float rate;             // Larger numbers mean the pitch,heading changes faster in order to seek the target
        public Matrix animRot;         // local rotation from animation system
        public Matrix untweakedBoneToWorld;
        public Matrix tweakedBoneToWorld;
        public Matrix tweakedBoneToParent;
        public Matrix tweakMatrix;     // matrix created from pitch and heading.  Applied after animRot (left of anim rot in a row matrix chain)
        public bool   tweakable;       // true if we can modify the rotation of this bone from a source other than animation

        public float dir;
        public Rotation(BoneEnum aBone, float aHeadingLimit, float aPitchLimit, float aJointRate, bool aTweakable)
        {
            bone = aBone;
            pitch = 0.0f;
            heading = 0.0f;
            headingLimit = aHeadingLimit;
            pitchLimit = aPitchLimit;
            rate = aJointRate;
            animRot = Matrix.Identity;
            untweakedBoneToWorld = Matrix.Identity;
            tweakedBoneToParent = Matrix.Identity;
            tweakMatrix = Matrix.Identity;
            tweakedBoneToParent = Matrix.Identity;
            this.tweakable = aTweakable;

            dir = 1.0f;  //debug var
        }
    }

    public class RotationChain
    {
        public Rotation[]   rotationList;
        public Matrix       firstBoneToWorld;

        public RotationChain(int n)
        {
            rotationList = new Rotation[n];
            firstBoneToWorld = Matrix.Identity;
        }

        public Rotation GetRotation(int i)
        {
            return rotationList[i];
        }

        public void ApplyAnimations(Matrix[] boneToParentList)
        {
            int rotationIndex;

            Matrix boneToWorld = firstBoneToWorld;

            for (rotationIndex = 0; rotationIndex < rotationList.Length; ++rotationIndex)
            {
                Rotation r = rotationList[rotationIndex];

                r.animRot = boneToParentList[(int) r.bone];
            }
        }

        public void AffectAnimations(ref List<Bone> boneList)
        {
            int rotationIndex;

            for (rotationIndex = 0; rotationIndex < rotationList.Length; ++rotationIndex)
            {
                Rotation r = rotationList[rotationIndex];

                // Must update all bones in chain even untweakables since
                // updates to world matrices have been made due to movement of tweakable 
                // bones.
                // Hopefully the animation from the next frame will override this
                // and so we are not ruining this bone.
                boneList[(int)r.bone].SetCompleteTransform( r.tweakedBoneToParent );
           }
        }

        /// <summary>
        /// Calc the forward kinematics by chaining the matrices from the start of the chain to the end
        /// </summary>
        public Matrix ForwardKinematics()
        {
            int i;

            Matrix boneToWorld = firstBoneToWorld;

            for (i = 0; i < rotationList.Length; ++i)
            {
                Rotation r = rotationList[i];

                r.tweakMatrix = rotateHeadingThenPitch( r.heading, r.pitch );
                r.tweakedBoneToParent = r.tweakMatrix * r.animRot;
                r.untweakedBoneToWorld =  r.animRot * boneToWorld;
                r.tweakedBoneToWorld = r.tweakMatrix * r.animRot * boneToWorld;
                boneToWorld = r.tweakedBoneToWorld;
            }

            // Returns end effector to world transform
            return boneToWorld;
        }

        public Matrix TweakedRotationToEffector(int rotationIndex)
        {
            int i;

            Matrix m = Matrix.Identity;

            for (i = rotationIndex + 1; i < rotationList.Length; ++i)
            {
                Rotation r = rotationList[i];

                m =  r.tweakedBoneToParent * m;
            }

            // Returns end rotation to effector matrix
            return m;

        }

        /// <summary>
        /// create a row matrix that is the result of rotation a frame about X by an amount "heading" and then 
        /// rotate about Z by an amount "pitch".  The coordinate system is right handed and follows the bones in the
        /// sample animation:  X - Up, Y - Forward, Z - left
        /// </summary>
        /// <param name="heading">
        /// amout to rotate about X axis in radians (follows right hand rule for sign)
        /// </param>
        /// <param name="pitch">
        /// amount to rotate about Z (after rotating X) in radians (follows right hand rule for sign) 
        /// </param>
        /// <returns>
        /// The matrix described in the summary
        /// </returns>
        protected Matrix rotateHeadingThenPitch(float heading, float pitch)
        {
            // This is coordinate system dependent here.  In the sample animation, X is up and Z is left and right handed system.
            return Matrix.CreateRotationZ(pitch) * Matrix.CreateRotationX(heading);
        }


    }

    public class HeadTracker
    {
        public RotationChain chain;

        /// <summary>
        /// HeadTracker contains the chain of joints that modify the animation system's matrices in order to point the 
        /// eyes at a target point.
        /// </summary>
        public HeadTracker()
        {
            int chainSize = 5;
            chain = new RotationChain(chainSize);

            chain.rotationList[--chainSize] = new Rotation(BoneEnum.leftEye, MathHelper.ToRadians(25.0f), MathHelper.ToRadians(30.0f), 3.0f, true);
            chain.rotationList[--chainSize] = new Rotation(BoneEnum.head, MathHelper.ToRadians(15.0f), MathHelper.ToRadians(25.0f), 1.0f, true);
            chain.rotationList[--chainSize] = new Rotation(BoneEnum.neck, MathHelper.ToRadians(15.0f), MathHelper.ToRadians(15.0f), 1.0f, true);
            chain.rotationList[--chainSize] = new Rotation(BoneEnum.spine3, MathHelper.ToRadians(15.0f), MathHelper.ToRadians(10.0f), 1.0f, true);
            chain.rotationList[--chainSize] = new Rotation(BoneEnum.spine2, MathHelper.ToRadians(15.0f), MathHelper.ToRadians(10.0f), 1.0f, true);

            // These are two test cases to make sure the math here matches the Mathematica math
            //Vector3 targetPoint = new Vector3(3.48752f, 42.6216278f, 2.82726f);
            //Vector3 translation = new Vector3(44.48259f, 3.271259f, 1.2745121f);
            //aimFunction(0.78539f, 0.09801f, targetPoint, Matrix.CreateTranslation(translation));
            //float gradX = 0.0f, gradZ = 0.0f;
            //gradient(0.78539f, 0.09801f, targetPoint, Matrix.CreateTranslation(translation), ref gradX, ref gradZ);
        }

        /// <summary>
        /// Called once per frame, it keeps the effector (eyes or neck) pointed at the target point.
        /// </summary>
        /// <param name="dt">
        /// delta time in seconds
        /// </param>
        /// <param name="targetPointWorldSpace">
        /// The point in world space to point the effector (eyes or neck) at.
        /// </param>
        /// <param name="boneToParentList">
        /// This is the list of transform matrices from the animation system that represent the current pose of the skeleton.  Each matrix
        /// transforms from its bone's local space to its parent's space.  Like matrix M_n_to_n-1.
        /// </param>
        /// <param name="boneToWorldList">
        /// List of transform matrices from the bone's local space to world space
        /// </param>
        /// <param name="trackWithEyes">
        /// set to true to point the eyes at the target but use the neck as the effector instead of the eyes.
        /// This means the character will look directly at the target.
        /// </param>
        public void Update(TimeSpan dt, Vector3 targetPointWorldSpace, Matrix[] boneToParentList, Matrix[] boneToWorldList)
        {
            float aim;
            bool moved = false; // true if a joint moved 
            int rotationIndex;

            // The chain starts at spine3 and is anchored to spine2
            chain.firstBoneToWorld = boneToWorldList[(int) BoneEnum.spine2];

            // Update all the internal matrices in the chain.  It is assumed that the update from the animation 
            // data happened already.
            chain.ForwardKinematics();

            // Now start updating the chain in the reverse direction, starting with the end effector.  
            // Iterate from end of the chain to the front covering each rotation
            for (rotationIndex = chain.rotationList.Length - 1; rotationIndex >= 0; --rotationIndex)
            {
                Rotation r = chain.rotationList[rotationIndex];

                if (r.tweakable)
                {
                    Matrix worldToUntweakedBone = Matrix.Invert(r.untweakedBoneToWorld);
                    Vector3 targetPointInUntweakedBoneSpace = Vector3.Transform(targetPointWorldSpace, worldToUntweakedBone );

                    Matrix effectorToUntweakedBone = Matrix.Invert(chain.TweakedRotationToEffector(rotationIndex));

                    moved = false;
                    // Move the rotation in the chain such that the end effector is aimed better at the target
                    // Indicate if it succeeded in moving toward the target and increasing the aim value.
                    aim = Track(dt, r, targetPointInUntweakedBoneSpace, effectorToUntweakedBone, ref moved);

                    // Debug code to prove that I am affecting the correct bone
                    //aim = 0.0f;
                    //if (r.bone == BoneEnum.leftEye || r.bone== BoneEnum.head )
                    //{
                    //    moved = false;
                    //    aim = 0.0f;

                    //    r.heading += r.dir * r.rate * (float)dt.TotalSeconds;
                    //    if (Math.Abs(r.heading * 180.0 / 3.1415) > 45)
                    //    {
                    //        r.dir = -r.dir;
                    //    }
                    //}

                     // The angles in the joint have changed so update the forward kinematics
                     chain.ForwardKinematics();

                    // If the aim is good enough we are done.
                    // If we moved in the right direction with this rotation then done for this frame.
                    if (aim > .98 )
                    {
                        r.pitch *= 0.98f;
                        r.heading *= 0.98f;
                        //break;
                    }
                    //if (moved)
                    //{
                    //   break;
                    //}

                }
            }
         }

        /// <summary>
        /// Tracks by adding pitch and heading rotation to a bone's existing transform.  It changes the boneToParent
        /// matrix of the bone.  This works by computing the aim, then the gradient of the aim.  It makes a single
        /// step in the direction that increases the aim function.  It doesn't exactly know how far of a step to
        /// take so that it doesn't overshoot the maximum and accidentally reduce the aim, so it tries its first
        /// guess and then makes sure that a step that far actually increased the aim.  If it doesn't increase the aim, it 
        /// reduces the step and tries again.
        /// </summary>
        /// <param name="dt">
        /// dt is delta time in seconds.  
        /// </param>
        /// <param name="joint">
        /// The joint being modified (actually its pitch, heading) in order to point the effector at the target.
        /// </param>
        /// <param name="targetPoint">
        /// The target point to point the effector at in the space unmodified joint space.  Think of it as the joint's
        /// pitch,heading creating a new space called the modified space and the space earlier in the chain from the base out
        /// to the effector is the unmodified space for the bone.
        /// </param>
        /// <param name="effectorToRotatedJoint">
        /// a transform from the effector's space to the unmodified space of the current joint.  If this matrix is identity
        /// then it means that this joint is the effector.
        /// </param>
        public float Track(TimeSpan dt, Rotation rotation, Vector3 targetPoint, Matrix effectorToRotatedJoint, ref bool moved)
        {
            // Compute the aim (-1...1)
            float aim = aimFunction(rotation.pitch, rotation.heading, targetPoint, effectorToRotatedJoint);
            float newAim = aim;
            moved = false;

            float jointRate = rotation.rate;
            int iterCount = 0;

            float dAimDPitch = 0.0f, dAimDHeading = 0.0f;
            // Compute the gradients
            gradient(rotation.pitch, rotation.heading, targetPoint, effectorToRotatedJoint, ref dAimDPitch, ref dAimDHeading);

            // We're really not that interested in the magnitude of the gradient, but rather the direction
            // to go that gives the maximum increase.
            float normGrad = (float)Math.Sqrt((double)(dAimDPitch * dAimDPitch + dAimDHeading * dAimDHeading));
            if (normGrad < 0.001f)
            {
                // we're already at a local maximum.  Can't do better than that.
                // Also, the division directly below might have blown up and caused problems.  Those bugs
                // are very hard to track down.
                return newAim;
            }
            // "Hat" is a convention in Physics signifying a unit vector.
            float dAimDHeadingHat = dAimDHeading / normGrad;
            float dAimDPitchHat = dAimDPitch / normGrad;

            while (iterCount++ < 4)
            {
                // Need to rotate the joint

                float pitchDelta = dAimDPitchHat * jointRate * (float)dt.TotalSeconds;
                float headingDelta = dAimDHeadingHat * jointRate * (float)dt.TotalSeconds;

                float newPitch = pitchDelta + rotation.pitch;
                float newHeading = headingDelta + rotation.heading;

                // Clip the joints.  Later this needs to be clipping to an ellipse
                if (ClipToRectangle(ref newHeading, ref newPitch, rotation.headingLimit, rotation.pitchLimit))
                {
                    // movement did not produce a better aim.  The maximum was hit.
                    moved = false;
                    break;
                }
                //ClipToEllipse(ref newHeading, ref newPitch, joint.headingLimit, joint.pitchLimit);

                newAim = aimFunction(newPitch, newHeading, targetPoint, effectorToRotatedJoint);
                if (newAim >= aim)
                {
                    // The step taken increases the aim so we're done here.
                    rotation.heading = newHeading;
                    rotation.pitch = newPitch;
                    moved = true;
                    break;
                }

                // The step taken didn't increase the aim.  That's bad.
                // Move half as fast and try again
                jointRate *= 0.5f;

            }
            return newAim;
        }

        /// <summary>
        /// create a row matrix that is the result of rotation a frame about X by an amount "heading" and then 
        /// rotate about Z by an amount "pitch".  The coordinate system is right handed and follows the bones in the
        /// sample animation:  X - Up, Y - Forward, Z - left
        /// </summary>
        /// <param name="heading">
        /// amout to rotate about X axis in radians (follows right hand rule for sign)
        /// </param>
        /// <param name="pitch">
        /// amount to rotate about Z (after rotating X) in radians (follows right hand rule for sign) 
        /// </param>
        /// <returns>
        /// The matrix described in the summary
        /// </returns>
        protected Matrix rotateHeadingThenPitch(float heading, float pitch)
        {
            // This is coordinate system dependent here.  In the sample animation, X is up and Z is left and right handed system.
            return Matrix.CreateRotationZ(pitch) * Matrix.CreateRotationX(heading);
        }

        /// <summary>
        /// "aim" is a quantity from -1 to 1.0 that represent how well the effector is pointing at the target point.  1.0 is perfect
        /// and -1 means it is pointing the opposite way.  Don't google it, I made up the name.
        /// </summary>
        /// <param name="pitch"></param>
        /// <param name="heading"></param>
        /// <param name="targetPoint"></param>
        /// <param name="effectorToRotatedJoint"></param>
        /// <returns></returns>
        public float aimFunction(float pitch, float heading, Vector3 targetPoint, Matrix effectorToRotatedJoint)
        {
            // Need to go from the variables in this class to the variables in the Mathematic worksheet.
            // Later this could be optimized but right now I need it to work.
            float ch, cp, sh, sp;

            ch = (float)Math.Cos(heading);
            sh = (float)Math.Sin(heading);
            cp = (float)Math.Cos(pitch);
            sp = (float)Math.Sin(pitch);

            Vector3 forwardLocalSpace = new Vector3(0.0f, 1.0f, 0.0f);    // forward to the bones in the animation. Not based on normal XNA coord.  Set by the way bones are in the animation.
            // Rotated joint space is joint space that is rotated by the head tracking pitch and heading
            Vector3 forwardRotatedJointSpace = Vector3.TransformNormal(forwardLocalSpace, effectorToRotatedJoint);
            float m10 = forwardRotatedJointSpace.X;
            float m11 = forwardRotatedJointSpace.Y;
            float m12 = forwardRotatedJointSpace.Z;
            Vector3 translation = effectorToRotatedJoint.Translation;
            float tx = translation.X;
            float ty = translation.Y;
            float tz = translation.Z;

            float px = targetPoint.X;
            float py = targetPoint.Y;
            float pz = targetPoint.Z;

            // aim = Dot((targetPoint - effectorBaseLocation)/Magnitude(targetPoint - effectorBaseLocation), effectorEndPoint - effectorBaseLocation) 
            // Mathematica works this out to:
            float aim = (px - cp * tx + sp * ty) * (-(cp * tx) + cp * (m10 + tx) + sp * ty - sp * (m11 + ty)) + (pz - sh * (sp * tx + cp * ty) - ch * tz) * (-(sh * (sp * tx + cp * ty)) + sh * (sp * (m10 + tx) + cp * (m11 + ty)) - ch * tz + ch * (m12 + tz)) +
   (py - ch * (sp * tx + cp * ty) + sh * tz) * (-(ch * (sp * tx + cp * ty)) + ch * (sp * (m10 + tx) + cp * (m11 + ty)) + sh * tz - sh * (m12 + tz));

            Vector3 targetPointMinusP0 = new Vector3(px - cp * tx + sp * ty, pz - sh * (sp * tx + cp * ty) - ch * tz, py - ch * (sp * tx + cp * ty) + sh * tz);
            aim /= targetPointMinusP0.Length();
            return aim;
        }

        // Some simple math functions I didn't find in the Math libraries.
        public float SqrtF(float x)
        {
            return (float)Math.Sqrt((double)x);
        }

        public float Sq(float x)
        {
            return x * x;
        }

        public float Cubed(float x)
        {
            return x * x * x;
        }

        public float PowerOnePointFive(float x)
        {
            return (float) Math.Sqrt((double) Cubed(x));
        }

        /// <summary>
        /// performs a gradient on the aim function.  This gradient will tell us which direction leads to the locally maximum value.
        /// It is a gradient of the aim function with respect to the joint's pitch and heading.  Those are the independent variables
        /// that are going to rotate the joint (and thus the effector) towards the target. Perhaps someone smarter than me knows a better than
        /// recursive descent to do this optimization calculation.
        /// This gradient is computed in Mathematica and is particular to a (Up, Forward, Left) coordinates for the joints as seen in the XNA 
        /// animation skinning sample code that I'm using.
        /// I used Mathematica and the CForm of an expression and cut/pasted it into here.
        /// </summary>
        public void gradient(float pitch, float heading, Vector3 targetPoint, Matrix effectorToRotatedJoint, ref float dAimDPitch, ref float dAimDHeading)
        {
            // Need to go from the variables in this class to the variables in the Mathematic worksheet.
            // Later this could be optimized but right now I need it to work.
            float ch, cp, sh, sp;

            ch = (float)Math.Cos(heading);
            sh = (float)Math.Sin(heading);
            cp = (float)Math.Cos(pitch);
            sp = (float)Math.Sin(pitch);

            Vector3 forwardLocalSpace = new Vector3(0.0f, 1.0f, 0.0f);    // forward to the bones in the animation. Not based on normal XNA coord.  Set by the way bones are in the animation.
            // Rotated joint space is joint space that is rotated by the head tracking pitch and heading
            Vector3 forwardRotatedJointSpace = Vector3.TransformNormal(forwardLocalSpace, effectorToRotatedJoint);
            float m10 = forwardRotatedJointSpace.X;
            float m11 = forwardRotatedJointSpace.Y;
            float m12 = forwardRotatedJointSpace.Z;
            Vector3 translation = effectorToRotatedJoint.Translation;
            float tx = translation.X;
            float ty = translation.Y;
            float tz = translation.Z;

            float px = targetPoint.X;
            float py = targetPoint.Y;
            float pz = targetPoint.Z;

            // Cut/pasted from Mathematica using CForm
            dAimDPitch = ((m12 * py * sh - cp * (m10 * px + m11 * pz * sh) + m11 * px * sp - m10 * pz * sh * sp - ch * (cp * m11 * py + m12 * pz + m10 * py * sp) + m10 * tx + m11 * ty + m12 * tz) *
      (2.0f *(sp*tx + cp*ty)*(px - cp*tx + sp*ty) + 2*(cp*sh*tx - sh*sp*ty)*(-pz + sh*sp*tx + cp*sh*ty + ch*tz) - 2*ch*(cp*tx - sp*ty)*(py - ch*(sp*tx + cp*ty) + sh*tz)))/
    (2.0f * PowerOnePointFive(Sq(px - cp*tx + sp*ty) + Sq(-pz + sh*sp*tx + cp*sh*ty + ch*tz) + Sq(py - ch*(sp*tx + cp*ty) + sh*tz))) - 
   (cp*m11*px - cp*m10*pz*sh + (m10*px + m11*pz*sh)*sp - ch*(cp*m10*py - m11*py*sp))/SqrtF(Sq(px - cp*tx + sp*ty) + Sq(-pz + sh*sp*tx + cp*sh*ty + ch*tz) + Sq(py - ch*(sp*tx + cp*ty) + sh*tz));

            dAimDHeading = ((m12 * py * sh - cp * (m10 * px + m11 * pz * sh) + m11 * px * sp - m10 * pz * sh * sp - ch * (cp * m11 * py + m12 * pz + m10 * py * sp) + m10 * tx + m11 * ty + m12 * tz) *
      (2.0f *(-pz + sh*sp*tx + cp*sh*ty + ch*tz)*(ch*sp*tx + ch*cp*ty - sh*tz) + 2*(sh*(sp*tx + cp*ty) + ch*tz)*(py - ch*(sp*tx + cp*ty) + sh*tz)))/
    (2.0f * PowerOnePointFive(Sq(px - cp * tx + sp * ty) + Sq(-pz + sh * sp * tx + cp * sh * ty + ch * tz) + Sq(py - ch * (sp * tx + cp * ty) + sh * tz))) - 
   (ch*m12*py - ch*cp*m11*pz - ch*m10*pz*sp + sh*(cp*m11*py + m12*pz + m10*py*sp))/SqrtF(Sq(px - cp*tx + sp*ty) + Sq(-pz + sh*sp*tx + cp*sh*ty + ch*tz) + Sq(py - ch*(sp*tx + cp*ty) + sh*tz));           
            
        }

        /// <summary>
        /// Limit a given cartesian point (x,y) to be within or on an ellipse.
        /// </summary>
        /// <param name="x">
        /// x value of cartesian point to be limited
        /// </param>
        /// <param name="y">
        /// y value of cartesian point to be limited
        /// </param>
        /// <param name="maxX">
        /// specifies the maximum value in the X direction.  This is the value 'a' in ellipse equations which is the semimajor axis (if maxX > maxY)
        /// </param>
        /// <param name="maxY">
        /// specifies the maximum value in the Y direction.  This is the value 'b' in ellipse equations which is the semiminor axis (if maxY < maxX )
        /// </param>
        public bool ClipToEllipse(ref float x, ref float y, float maxX, float maxY)
        {
            bool maxHit = false;

            // Convert the input point (x,y) to polar form (r, theta)
            float theta = (float) Math.Atan2(y, x);
            float r = SqrtF(x * x + y * y);
            // Compute the maximum radius as the point on the ellipse with the same angle theta
            float rMax = SqrtF(Sq(maxX * (float)Math.Cos(theta)) + Sq(maxY * (float)Math.Sin(theta)));

            if (r > rMax)
            {
                // Clipping required.  The clipped point is the point on the ellipse
                x = maxX * (float) Math.Cos(theta);
                y = maxY * (float) Math.Sin(theta);
                maxHit = true;
            }
            return maxHit;
        }

        /// <summary>
        /// Limit a given cartesian point (x,y) to be within or on a rectangle.
        /// </summary>
        /// <param name="x">
        /// x value of cartesian point to be limited
        /// </param>
        /// <param name="y">
        /// y value of cartesian point to be limited
        /// </param>
        /// <param name="maxX">
        /// specifies the maximum value in the X direction. 
        /// </param>
        /// <param name="maxY">
        /// specifies the maximum value in the Y direction.
        /// </param>
        public bool ClipToRectangle(ref float x, ref float y, float maxX, float maxY)
        {
            bool maxHit = false;

            if (x > maxX)
            {
                x = maxX;
                maxHit = true;
            }
            else if (x < -maxX)
            {
                x = -maxX;
                maxHit = true;
            }
            if (y > maxY)
            {
                y = maxY;
                maxHit = true;
            }
            else if (y < -maxY)
            {
                y = -maxY;
                maxHit = true;
            }
            return maxHit;
        }

        /// <summary>
        /// Checks if this class modifies the matrix for the given bone and returns the transform if it does.
        /// This is how this system interfaces with the animation system as a whole.
        /// </summary>
        /// <param name="boneIndex">
        /// Index of the bone in the skeleton to check.  Not the index of the rotation in the chain.
        /// </param>
        /// <returns>
        /// a rotation matrix for the bone to tack onto the bone's transformation.
        /// </returns>
        public Matrix Modifier(int boneIndex)
        {
            foreach (Rotation r in chain.rotationList)
            {
                if ((int)r.bone == boneIndex)
                {
                    return r.tweakMatrix;
                }
            }

            // We're syncing the eyes so this is a special case
            if (boneIndex == (int)BoneEnum.rightEye)
            {
                return chain.rotationList[3].tweakMatrix;   // hack
            }
            return Matrix.Identity;
         }

    }
}
