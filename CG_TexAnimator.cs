using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace CG_CharacterSkinner
{
    // Job component system allows for the use of ECS specific jobs
    public class CG_TexAnimator : JobComponentSystem
    {
        // pre determine the array and batch sizes
        public const int AnimArraySize = 1250;
        public const int BatchSize = 4;

        // The structure for the clip data
        public struct AnimationClipDataBaked
        {
            public float textureOfset;
            public float textureRange;
            public float texturePixelOffset;
            public int textureWidth;

            public float animationLength;
            public BlittableBool bAnimationLoops;
        }

        // Contains the information based on the character data
        public struct CharacterData
        {
            public ComponentDataArray<AnimationData> animationData;
            public ComponentDataArray<CharacterTransformData> transforms;
            public readonly int Length;
        }

        // The data required for the texture animation
        public CG_KeyframeBaker.AnimationData BakedData;
        public CG_CharacterDrawer Drawer;
        public Material Material;

        // Inject allows for the easy gathering of all entities with the same attributes
        [Inject]
        private CharacterData characters;

        // A NativeArray exposes a buffer of native memory to managed code, making it possible to share data between managed and native without marshalling costs.
        private NativeArray<AnimationClipDataBaked> animationClipBakedData;
        private JobHandle previousFrameJob;

        // used to check whether the tex animator is initialised to stop
        // multiple initialisations
        public bool bInit = false;

        // IJobParrelFor allows for the same operation to occur for each item in a native container
        struct PrepareAnimatorDataJob : IJobParallelFor
        {
            // set the length of the array size, make read only 
            [NativeFixedLength(AnimArraySize)] [ReadOnly]
            public NativeArray<AnimationClipDataBaked> animationClips;

            // the delta time
            public float deltaTime;

            // the animation data
            public ComponentDataArray<AnimationData> texAnimData;

            // Called when the structure is accessed
            public void Execute(int i)
            {
                // Pass through the animation data from the user
                CG_GameManager UserAnimationData = CG_GameManager.Instance;

                // The current texture animator data at the index provide
                AnimationData animData = texAnimData[i];

                // if the animation id isnt the same as the new animation data
                if (animData.animationID != animData.newAnimationId)
                {
                    // transition pre anim
                    if (animData.animationID >= 0)
                    {
                        // set pre-transition data values
                        animData.animationToSwitchID = animData.animationID;
                        animData.animationToSwitchNormTime = animData.normalizedAnimationTime;
                        animData.transitionPercent = 0f;

                        // get the animation loops based on the user inputted varaibles
                        if (animData.animationID == 1)
                            animData.animationToSwitchLooping = UserAnimationData.LoopAnimation2;
                        else
                            animData.animationToSwitchLooping = UserAnimationData.LoopAnimation1;
                    }
                    else
                        animData.transitionPercent = -1f;

                    // set the animation id and resent the normalised time
                    animData.animationID = animData.newAnimationId;
                    animData.normalizedAnimationTime = 0f;
                }

                // Geth the baked clip data from the current animation index
                AnimationClipDataBaked bakedClip = animationClips[animData.animationID];

                // Set the default normalised time of the animation
                float normalizedTime = animData.normalizedAnimationTime + deltaTime / (bakedClip.animationLength / UserAnimationData.Animation1Speed);

                // Change the speed dependant on the animation clip being played
                if (animData.animationID == 1)
                    normalizedTime = animData.normalizedAnimationTime + deltaTime / (bakedClip.animationLength / UserAnimationData.Animation2Speed);

                // if the normalised time is equal to 1, the animation has
                if (normalizedTime > 1.0f)
                {
                    // Get whether the animation should loop or not based on user variables
                    bool animLoop = UserAnimationData.LoopAnimation1;

                    if (animData.animationID == 1)
                        animLoop = UserAnimationData.LoopAnimation2;

                    // if the animation should loop, reset the normalised time
                    // else hold this value at 1 
                    if (animLoop)
                        normalizedTime = 0;
                    else
                        normalizedTime = 1f;
                }

                // set the animation datas normalised time to this new normalised time
                animData.normalizedAnimationTime = normalizedTime;

                // if a transition is in progress
                if (animData.transitionPercent >= 0.0f)
                {
                    float blendPercent = animData.transitionPercent + deltaTime / UserAnimationData.TransitionSpeed;

                    // Transition ended
                    if (blendPercent > 1f)
                        blendPercent = -1f;
                    else
                    {
                        // continue preanimation
                        AnimationClipDataBaked preClip = animationClips[animData.animationToSwitchID];
                        float preNormalizedTime = animData.animationToSwitchNormTime + deltaTime / (preClip.animationLength);

                        // if the animation should loop, reset the normalised time
                        // else hold this value at 1 
                        if (preNormalizedTime > 1.0f)
                        {
                            if (animData.animationToSwitchLooping)
                                preNormalizedTime = 0;
                            else
                                preNormalizedTime = 1f;
                        }
                        animData.animationToSwitchNormTime = preNormalizedTime;
                    }
                    animData.transitionPercent = blendPercent;
                }
                texAnimData[i] = animData;
            }
        }

        // This structure will be burst compiled
        [BurstCompile]
        //IJob allows you to schedule a single job that runs in parallel to other jobs and the main thread.
        struct ComputeJobParams : IJob
        {
            // The animation data from the keyframe baker
            [ReadOnly] public ComponentDataArray<AnimationData> animData;

            // The positional data of the character
            [ReadOnly] public ComponentDataArray<CharacterTransformData> charTransforms;

            // The animation size of the array
            [NativeFixedLength(AnimArraySize)] [ReadOnly] public NativeArray<AnimationClipDataBaked> animClips;

            // Creation of the native list variables
            public NativeList<float4> characterPositions;
            public NativeList<quaternion> characterRotations;
            public NativeList<float3> texturePositions;
            public NativeList<float3> preAnimationTexturePositions;
            public NativeList<float> animationTransitionProgress;
        
            public void Execute()
            {
                // Loop through the number of character transform positions
                for (int i = 0; i < charTransforms.Length; i++)
                {
                    // Get the current animation and transform based on the character selected
                    AnimationData animation = animData[i];
                    CharacterTransformData characterTransform = charTransforms[i];

                    // Get the animation clip from the current animation
                    AnimationClipDataBaked animClip = animClips[animation.animationID];

                    // The position of the animation position
                    float animTexturePos = animData[i].normalizedAnimationTime * animClip.textureRange + animClip.textureOfset;

                    // The position of centre pixel locations 
                    float lowerCentreTexPos = ((int)math.floor(animTexturePos * animClip.textureWidth) * 1.0f) / animClip.textureWidth;
                    float upperCentreTexPos = lowerCentreTexPos + animClip.texturePixelOffset;
                    float3 texturePos = new float3(lowerCentreTexPos, upperCentreTexPos, (animTexturePos - lowerCentreTexPos) / animClip.texturePixelOffset);

                    // Animation transition progress
                    float transitionPercent = animation.transitionPercent;

                    // Pre animation texture (transition)
                    AnimationClipDataBaked preClip = animClips[animation.animationToSwitchID];
                    float preTexturePosition = animData[i].animationToSwitchNormTime * preClip.textureRange + preClip.textureOfset;
                    int preLowerPixelInt = (int)math.floor(preTexturePosition * preClip.textureWidth);

                    // Do this before the new animation 
                    float preLowerCentreTexPos = (preLowerPixelInt * 1.0f) / preClip.textureWidth;
                    float preUpperCentreTexPos = preLowerCentreTexPos + preClip.texturePixelOffset;
                    float3 preTexturePos = new float3(preLowerCentreTexPos, preUpperCentreTexPos, (preTexturePosition - preLowerCentreTexPos) / preClip.texturePixelOffset);

                    // Add the positional and rotation character data to their respective native lists
                    characterPositions.Add(new float4(characterTransform.Position, 1));
                    characterRotations.Add(quaternion.LookRotation(characterTransform.Forward, new Vector3(0.0f, 1.0f, 0.0f)));

                    // Add the texture animation data to their respecitve native lists
                    texturePositions.Add(texturePos);
                    preAnimationTexturePositions.Add(preTexturePos);
                    animationTransitionProgress.Add(transitionPercent);
                }
            }
        }

        // Initialise the texture animator
        private void InitializeTextureAnimator()
        {
            // if initialised, return to stop multiple initialisationss
            if (bInit) return;

            // Create the clip baked data variable with a persistant allocator
            animationClipBakedData = new NativeArray<AnimationClipDataBaked>(AnimArraySize, Allocator.Persistent);

            // Get the characterSkining prefab located in the game manager
            GameObject charPrefab = CG_GameManager.Instance.GetCharacterPrefab();

            // if a character prefab exists
            if (charPrefab)
            {
                // Get the character data 
                CG_CharacterSkinner charSkinner = charPrefab.GetComponentInChildren<CG_CharacterSkinner>();
                GameObject bakingChar = GameObject.Instantiate(charSkinner.characterToSkin);
                SkinnedMeshRenderer characterSkinnedMesh = bakingChar.GetComponentInChildren<SkinnedMeshRenderer>();

                // Get the baking data from the skinned mesh renderer of the character and the material
                BakedData = CG_KeyframeBaker.BakeAnimation(characterSkinnedMesh);
                Material = charSkinner.characterMat;

                // initilise the character drawer class
                Drawer = new CG_CharacterDrawer(this, BakedData.NewMesh);

                // loop through the number of animation clips
                for (int i = 0; i < BakedData.animationClipData.Count; i++)
                {
                    // set the animation clip data to each animation
                    AnimationClipDataBaked data = new AnimationClipDataBaked();
                    data.animationLength = BakedData.animationClipData[i].Clip.length;

                    // Get the texture range and offset and return the range, offset, pixel offset and widths of the texture
                    DataRangeOffsetWidth(BakedData, BakedData.animationClipData[i], out data.textureRange, out data.textureOfset, out data.texturePixelOffset, out data.textureWidth);

                    // Get whether the animation should loop
                    data.bAnimationLoops = BakedData.animationClipData[i].Clip.isLooping;
                    animationClipBakedData[(int)0 * 25 + i] = data;
                }

                // Destroy the baking character
                GameObject.Destroy(bakingChar);
            }

            // Initialisation is complete
            bInit = true;
        }

        // Calculates the texture range, offset, pixel offset and width of the animation data
        private void DataRangeOffsetWidth(CG_KeyframeBaker.AnimationData animData, CG_KeyframeBaker.AnimationClipData animClipData, out float range, out float textureOfset, out float texturePixelOffset, out int textureWidth)
        {
            float start = (float)animClipData.textureStart / animData.textures[0].width + (1f / animData.textures[0].width) * 0.5f;
            float end = (float)animClipData.textureEnd / animData.textures[0].width + (1f / animData.textures[0].width) * 0.5f;
            texturePixelOffset = (1f / animData.textures[0].width);
            textureWidth = animData.textures[0].width;
            range = end - start;
            textureOfset = start;
        }

        // Called when the manager is destoryed 
        protected override void OnDestroyManager()
        {
            // Flush the job system
            previousFrameJob.Complete();

            // Call the dispose function on the character drawer and baked data
            if (Drawer != null) Drawer.Dispose();
            if (animationClipBakedData.IsCreated) animationClipBakedData.Dispose();

            // Call the base functiion
            base.OnDestroyManager();
        }

        // On the updating of the tex animator
        protected override JobHandle OnUpdate(JobHandle dependancies)
        {
            // initialise the system for the first ime
            InitializeTextureAnimator();
            if (!bInit) return dependancies;

            // flush the job system
            previousFrameJob.Complete();
            previousFrameJob = dependancies;

            // Draw the instanced meshes
            Drawer.DrawMeshes();

            // Set the animator job attributes
            PrepareAnimatorDataJob prepareAnimatorJob = new PrepareAnimatorDataJob();
            prepareAnimatorJob.animationClips = animationClipBakedData;
            prepareAnimatorJob.deltaTime = Time.deltaTime;
            prepareAnimatorJob.texAnimData = characters.animationData;

            // Schedule this job dependant on the previous job
            JobHandle prepareAnimator = prepareAnimatorJob.Schedule(characters.Length, BatchSize, previousFrameJob);

            // Create a tempory job handle array 
            NativeArray<JobHandle> jobHandles = new NativeArray<JobHandle>(50, Allocator.Temp);

            // set the firs t job handle to the prepaer animator job
            jobHandles[0] = prepareAnimator;

            ComputeJobs(prepareAnimator, jobHandles);
            previousFrameJob = JobHandle.CombineDependencies(jobHandles);

            // dispose of the jobs
            jobHandles.Dispose();
            return previousFrameJob;
        }

        private void ComputeJobs(JobHandle previousJob, NativeArray<JobHandle> jobHandles)
        {
            // clear the character drawer native arrays
            Drawer.ClearNativeArrays();

            // Set the compute job variables
            ComputeJobParams computeJobParams = new ComputeJobParams();
            computeJobParams.charTransforms = characters.transforms;
            computeJobParams.animData = characters.animationData;
            computeJobParams.animClips = animationClipBakedData;
            computeJobParams.characterPositions = Drawer.characterPositionsList;
            computeJobParams.characterRotations = Drawer.characterRotationsList;
            computeJobParams.texturePositions = Drawer.texCoord;
            computeJobParams.preAnimationTexturePositions = Drawer.preAnimtexCoord;
            computeJobParams.animationTransitionProgress = Drawer.blendPercent;

            // prepare the shader jobs
            JobHandle shaderJob = computeJobParams.Schedule(previousJob);
            // set the first job to the shader job
            jobHandles[0] = shaderJob;
        }
    }
}