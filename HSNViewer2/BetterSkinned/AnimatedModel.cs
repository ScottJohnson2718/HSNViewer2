using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using AnimationAux;

namespace BetterSkinned
{
    /// <summary>
    /// An encloser for an XNA model that we will use that includes support for
    /// bones, animation, and some manipulations.
    /// </summary>
    public class AnimatedModel
    {
        #region Fields

        /// <summary>
        /// The actual underlying XNA model
        /// </summary>
        private Model model = null;

        /// <summary>
        /// Extra data associated with the XNA model
        /// </summary>
        private ModelExtra modelExtra = null;

        /// <summary>
        /// The model bones
        /// </summary>
        private List<Bone> bones = new List<Bone>();

        /// <summary>
        /// The model asset name
        /// </summary>
        private string assetName = "";

        /// <summary>
        /// An associated animation clip player
        /// </summary>
        private AnimationPlayer player = null;

        #endregion

        #region Properties

        /// <summary>
        /// The actual underlying XNA model
        /// </summary>
        public Model Model
        {
            get { return model; }
        }

        /// <summary>
        /// The underlying bones for the model
        /// </summary>
        public List<Bone> Bones { get { return bones; } }

        /// <summary>
        /// The model animation clips
        /// </summary>
        public List<AnimationClip> Clips { get { return modelExtra.Clips; } }


        HeadTracker headTracker;

        #endregion

        #region Construction and Loading

        /// <summary>
        /// Constructor. Creates the model from an XNA model
        /// </summary>
        /// <param name="assetName">The name of the asset for this model</param>
        public AnimatedModel(string assetName)
        {
            this.assetName = assetName;

            headTracker = new HeadTracker();
        }

        /// <summary>
        /// Load the model asset from content
        /// </summary>
        /// <param name="content"></param>
        public void LoadContent(ContentManager content)
        {
            this.model = content.Load<Model>(assetName);
            modelExtra = model.Tag as ModelExtra;
            System.Diagnostics.Debug.Assert(modelExtra != null);

            ObtainBones();
        }


        #endregion

        #region Bones Management

        /// <summary>
        /// Get the bones from the model and create a bone class object for
        /// each bone. We use our bone class to do the real animated bone work.
        /// </summary>
        private void ObtainBones()
        {
            bones.Clear();
            foreach (ModelBone bone in model.Bones)
            {
                // Create the bone object and add to the heirarchy
                Bone newBone = new Bone(bone.Name, bone.Transform, bone.Parent != null ? bones[bone.Parent.Index] : null);

                // Add to the bones for this model
                bones.Add(newBone);
            }
        }

        /// <summary>
        /// Find a bone in this model by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Bone FindBone(string name)
        {
            foreach(Bone bone in Bones)
            {
                if (bone.Name == name)
                    return bone;
            }

            return null;
        }

        #endregion

        #region Animation Management

        /// <summary>
        /// Play an animation clip
        /// </summary>
        /// <param name="clip">The clip to play</param>
        /// <returns>The player that will play this clip</returns>
        public AnimationPlayer PlayClip(AnimationClip clip)
        {
            // Create a clip player and assign it to this model
            player = new AnimationPlayer(clip, this);
            return player;
        }

        #endregion

        #region Updating

        /// <summary>
        /// Update animation for the model.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime, Vector3 targetPointWorldSpace)
        {
            if (player != null)
            {
                player.Update(gameTime);

                Matrix[] boneToWorldTransforms = new Matrix[bones.Count];
                Matrix[] boneToParentTransforms = new Matrix[bones.Count];

                // Concat all the matrices together without any tweaks from the head tracking
                for (int i = 0; i < bones.Count; i++)
                {
                    Bone bone = bones[i];

                    bone.ComputeAbsoluteTransform();

                    boneToWorldTransforms[i] = bone.BoneToWorld();
                    boneToParentTransforms[i] = bone.BoneToParent();
                }

                // Tell the head tracker about the latest animation rotations
                headTracker.chain.ApplyAnimations(boneToParentTransforms);

                // Let the head tracker move the end effector towards the target.
                // It hasn't affected the rotations of the bones yet, just the surrogate bones in the chain.
                headTracker.Update(gameTime.ElapsedGameTime, targetPointWorldSpace, boneToParentTransforms, boneToWorldTransforms);

                // Use the results of the chain as an external source of animation to the bones.  This
                // clobbers the actual animation data on the bones in the chain until the end of the frame.
                // OK, "clobbers" is a little harsh considering that the rotations for the chain still contain
                // the animation data.  Those animations have been tweaked with extra rotations.
                headTracker.chain.AffectAnimations(ref bones);

                // Concat all the matrices now that the tweak rotations are in there.
                for (int i = 0; i < bones.Count; i++)
                {
                    Bone bone = bones[i];

                    bone.ComputeAbsoluteTransform();
               }

                // Update everything in the bone tree from the first bone in the chain
                // Trying to get both eyes to move and all the fingers on the hands even though
                // they are not part of the head tracking chain.
                //bones[0].ForwardKinematics(Matrix.Identity);
            }
        }

        #endregion

        #region Drawing

        /// <summary>
        /// Draw the model
        /// </summary>
        /// <param name="graphics">The graphics device to draw on</param>
        /// <param name="camera">A camera to determine the view</param>
        /// <param name="world">A world matrix to place the model</param>
        public void Draw(GraphicsDevice graphics, Camera camera, Matrix world, Axes axes)
        {
            if (model == null)
                return;

            //
            // Compute all of the bone absolute transforms
            //

            Matrix[] boneTransforms = new Matrix[bones.Count];

            for (int i = 0; i < bones.Count; i++)
            {
                Bone bone = bones[i];

                // Reusing the result from the Update function
                boneTransforms[i] = bone.AbsoluteTransform;
            }

            //
            // Determine the skin transforms from the skeleton
            //

            Matrix[] skeleton = new Matrix[modelExtra.Skeleton.Count];
            for (int s = 0; s < modelExtra.Skeleton.Count; s++)
            {
                int boneIndex = modelExtra.Skeleton[s];
                Bone bone = bones[modelExtra.Skeleton[s]];
                //skeleton[s] = bone.SkinTransform * bone.AbsoluteTransform;
                skeleton[s] = bone.SkinTransform * boneTransforms[boneIndex];
            }

            // Draw the model.
            foreach (ModelMesh modelMesh in model.Meshes)
            {
                foreach (Effect effect in modelMesh.Effects)
                {
                    if (effect is BasicEffect)
                    {
                        BasicEffect beffect = effect as BasicEffect;
                        beffect.World = boneTransforms[modelMesh.ParentBone.Index] * world;
                        beffect.View = camera.View;
                        beffect.Projection = camera.Projection;
                        beffect.EnableDefaultLighting();
                        beffect.PreferPerPixelLighting = true;
                    }

                    if (effect is SkinnedEffect)
                    {
                        SkinnedEffect seffect = effect as SkinnedEffect;
                        seffect.World = boneTransforms[modelMesh.ParentBone.Index] * world;
                        seffect.View = camera.View;
                        seffect.Projection = camera.Projection;
                        seffect.EnableDefaultLighting();
                        seffect.PreferPerPixelLighting = true;
                        seffect.SetBoneTransforms(skeleton);
                    }
                }

                modelMesh.Draw();
            }

            //axes.Draw(camera.View, headTracker.chain.rotationList[3].tweakedBoneToWorld, camera.Projection);
            //axes.Draw(camera.View, headTracker.chain.rotationList[2].tweakedBoneToWorld, camera.Projection);

            for (int rotationIndex = 0; rotationIndex < headTracker.chain.rotationList.Length; ++rotationIndex)
            {
                if (headTracker.chain.rotationList[rotationIndex].tweakable)
                {
                    headTracker.chain.rotationList[rotationIndex].tweakedBoneToWorld.Translation += new Vector3(-10.0f, 0.0f, 0.0f);
                    axes.Draw(camera.View, headTracker.chain.rotationList[rotationIndex].tweakedBoneToWorld, camera.Projection);

                    if (headTracker.chain.rotationList[rotationIndex].contributed)
                    {
                        headTracker.chain.rotationList[rotationIndex].tweakedBoneToWorld.Translation += new Vector3(20.0f, 0.0f, 0.0f);
                        axes.Draw(camera.View, headTracker.chain.rotationList[rotationIndex].tweakedBoneToWorld, camera.Projection);
                    }
                }
            }
 
        }


        #endregion

    }
}
