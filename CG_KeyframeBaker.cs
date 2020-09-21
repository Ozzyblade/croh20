using System.Collections.Generic;
using UnityEngine;

namespace CG_CharacterSkinner
{
    public static class CG_KeyframeBaker
    {
        // The structure that handles the data created by the
        // animations
        public class AnimationData
        {
            public Texture2D[] textures;
            public Mesh NewMesh;
            public List<AnimationClipData> animationClipData = new List<AnimationClipData>();
        }

        // Data refering to animation clips
        public class AnimationClipData
        {
            public AnimationClip Clip;
            public int textureStart;
            public int textureEnd;
        }

        // Helper script, takes a mesh and copys all the component
        // data values into a new mesh
        public static void CopyMesh(this Mesh meshToCopy, Mesh newMesh)
        {
            newMesh.vertices = meshToCopy.vertices;
            newMesh.triangles = meshToCopy.triangles;
            newMesh.normals = meshToCopy.normals;
            newMesh.uv = meshToCopy.uv;
            newMesh.tangents = meshToCopy.tangents;
            newMesh.name = meshToCopy.name;
        }

        // This function handles the conversion of a skinned mesh renderer into standard mesh data
        private static Mesh ConvertSkinnedMesh(SkinnedMeshRenderer meshToSkin)
        {
            // The new mesh and the shared mesh of the skinned mesh renderer
            Mesh staticMesh = new Mesh();
            Mesh skinnedSharedMesh = meshToSkin.sharedMesh;
            BoneWeight[] skinnedBoneWeight = skinnedSharedMesh.boneWeights;

            // copy the original mesh into the new mesh
            skinnedSharedMesh.CopyMesh(staticMesh);

            // initialise bone data variables, these are the 2nd and 3rd
            // texture coordiantes of the mesh
            Vector2[] meshUV2 = new Vector2[staticMesh.vertexCount];
            Vector2[] meshUV3 = new Vector2[staticMesh.vertexCount];

            // Loop through the number of vertexes in the original mesh
            for (int i = 0; i < skinnedSharedMesh.vertexCount; i++)
            {
                // set the value of these texture coordinates of the mesh
                meshUV2[i] = new Vector2((skinnedBoneWeight[i].boneIndex0 + 0.5f) / meshToSkin.bones.Length, (skinnedBoneWeight[i].boneIndex1 + 0.5f) / meshToSkin.bones.Length);
                meshUV3[i] = new Vector2(skinnedBoneWeight[i].weight0 / (skinnedBoneWeight[i].weight0 + skinnedBoneWeight[i].weight1), skinnedBoneWeight[i].weight1 / (skinnedBoneWeight[i].weight0 + skinnedBoneWeight[i].weight1));
            }

            // Set the second and third texture coordinates of this new
            // static mesh
            staticMesh.uv2 = meshUV2;
            staticMesh.uv3 = meshUV3;

            // return this new mesh
            return staticMesh;
        }

        // Main functioon, handles the baking of the animation
        public static AnimationData BakeAnimation(SkinnedMeshRenderer meshToSample)
        {
            // Create the default variables
            AnimationData bakedAnimData = new AnimationData();
            List<Matrix4x4[,]> boneMatrix = new List<Matrix4x4[,]>();
            int keyframes = 0;

            // Convert the skinned mesh to sample into a static mesh
            bakedAnimData.NewMesh = ConvertSkinnedMesh(meshToSample);

            // Get the animations from the user
            CG_GameManager UserAnimations = CG_GameManager.Instance;

            // Assign these animation clips. For this to work, the animations must be inserted into
            // the mesh renderers animator controller, however allows the hot swapping of animations
            AnimationClip[] animationClips = new AnimationClip[2];

            // If the animations are found, add these to the clips
            if (UserAnimations.Animation1)
                animationClips[0] = UserAnimations.Animation1;

            if (UserAnimations.Animation2)
                animationClips[1] = UserAnimations.Animation2;

            // For both of the animation clips, sample their data 
            for (int i = 0; i < animationClips.Length; i++)
            {
                Matrix4x4[,] sampledAim = SampleAnimation(animationClips[i], meshToSample);
                boneMatrix.Add(sampledAim);
                keyframes += sampledAim.GetLength(0);
            }

            // Initialise the texture data of the baked data
            bakedAnimData.textures = new Texture2D[3];

            for (int i = 0; i < bakedAnimData.textures.Length; i++)
                bakedAnimData.textures[i] = new Texture2D(keyframes, boneMatrix[0].GetLength(1), TextureFormat.RGBAFloat, false);

            // Initialise the texture color data of the baked data
            int tCW = bakedAnimData.textures[0].width * bakedAnimData.textures[0].height;
            Color[] texture0Colour = new Color[tCW];
            Color[] texture1Colour = new Color[tCW];
            Color[] texture2Colour = new Color[tCW];

            // loop through and assign this colour data
            int totalKeyframes = 0;

            for (int i = 0; i < boneMatrix.Count; i++)
            {
                for (int boneIdx = 0; boneIdx < boneMatrix[i].GetLength(1); boneIdx++)
                {
                    for (int keyframeIdx = 0; keyframeIdx < boneMatrix[i].GetLength(0); keyframeIdx++)
                    {
                        int index = (boneIdx * bakedAnimData.textures[0].width + (totalKeyframes + keyframeIdx));

                        texture0Colour[index] = boneMatrix[i][keyframeIdx, boneIdx].GetRow(0);
                        texture1Colour[index] = boneMatrix[i][keyframeIdx, boneIdx].GetRow(1);
                        texture2Colour[index] = boneMatrix[i][keyframeIdx, boneIdx].GetRow(2);
                    }
                }

                // set the color data
                bakedAnimData.textures[0].SetPixels(texture0Colour);
                // Apply this without setting minmaps and do not make them no longer readable
                bakedAnimData.textures[0].Apply(false, false);
                bakedAnimData.textures[1].SetPixels(texture1Colour);
                bakedAnimData.textures[1].Apply(false, false);
                bakedAnimData.textures[2].SetPixels(texture2Colour);
                bakedAnimData.textures[2].Apply(false, false);

                // Create the animation clip data
                AnimationClipData clipData = new AnimationClipData();
                clipData.Clip = animationClips[i];
                clipData.textureStart = totalKeyframes + 1;
                clipData.textureEnd = (totalKeyframes + boneMatrix[i].GetLength(0) - 1)-1;

                // Add this animation to the animation clip data
                bakedAnimData.animationClipData.Add(clipData);

                // count the total running amount of keyframes
                totalKeyframes += boneMatrix[i].GetLength(0);
            }

            // Return the AnimatorData gathered
            return bakedAnimData;
        }

        // Function loops through the bind poseses of the animations and stores their location matrix
        private static Matrix4x4[,] SampleAnimation(AnimationClip animClip, SkinnedMeshRenderer prefabSkinnedMesh)
        {
            // Get the animator in the parent the skinned mesh
            Animator animator = prefabSkinnedMesh.GetComponentInParent<Animator>();

            // initialise the bone matrices variable
            Matrix4x4[,] bonePosMatrix = new Matrix4x4[Mathf.CeilToInt(60 * animClip.length) + 3, prefabSkinnedMesh.bones.Length];

            // Set the delta time of this clip, this is used for gathering the positional values of the mesh
            float deltaTime = animClip.length / (bonePosMatrix.GetLength(0) - 1);

            // loop through the number of bones of the mesh in the 1st dimension
            for (int i = 1; i < bonePosMatrix.GetLength(0) - 1; i++)
            {      
                // Play the animation located in the animator controller with the same name as the 
                // user chosen animations. The layer and the normalised time
                animator.Play(animClip.name, -1, (float)(i - 1) / (bonePosMatrix.GetLength(0) - 3));
                // evalute the animator position based on the data time inserted
                animator.Update(deltaTime);

                // loop through the skinned mesh renderes bones and set them equal to the animation mesh bind pose.
                for (int j = 0; j < prefabSkinnedMesh.bones.Length; j++)
                    bonePosMatrix[i, j] = prefabSkinnedMesh.localToWorldMatrix.inverse * prefabSkinnedMesh.bones[j].localToWorldMatrix * prefabSkinnedMesh.sharedMesh.bindposes[j];
            }

            for (int j = 0; j < prefabSkinnedMesh.bones.Length; j++)
            {
                bonePosMatrix[0, j] = bonePosMatrix[bonePosMatrix.GetLength(0) - 2, j];
                bonePosMatrix[bonePosMatrix.GetLength(0) - 1, j] = bonePosMatrix[1, j];
            }

            // Return these bone matrices
            return bonePosMatrix;
        }
    }
}