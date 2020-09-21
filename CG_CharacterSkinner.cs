/** This script is responsible for the skinning of a generic animated character */
using UnityEngine;
using Unity.Entities;
using System;
using Unity.Mathematics;

namespace CG_CharacterSkinner
{
    // As bool isn't a blittable type, it cannot work with IComponentData,
    // therefore we must a custom solution

    // Taken from the Unity Forum - Poster Xisor
    // https://forum.unity.com/threads/alternative-to-boolean-in-icomponentdata-blittable-datatypes.523986/

    public struct BlittableBool
    {
        private readonly byte Value;

        public BlittableBool(bool value) { Value = (byte)(value ? 1 : 0); }
        public static implicit operator BlittableBool(bool value) { return new BlittableBool(value); }
        public static implicit operator bool (BlittableBool value) { return value.Value != 0; }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public struct AnimationData : IComponentData
    {
        // Before the new animation starts
        public int animationToSwitchID;
        public float animationToSwitchNormTime;
        public BlittableBool animationToSwitchLooping;

        // The new animation data
        public int newAnimationId;

        // Current animation variables
        public float normalizedAnimationTime;
        public int animationID;

        // How far through the transition the animation is
        public float transitionPercent;
    }

    // This structure holds the data as to where the entity
    // will be positioned
    public struct CharacterTransformData : IComponentData
    {
        public float3 Position;
        public float3 Forward;
    }

    // Ensure that the game object entity is placed in the inspector
    [RequireComponent(typeof(GameObjectEntity))]
    public class CG_CharacterSkinner : MonoBehaviour {

        private EntityManager entityManager;
        public Entity entity;

        [Header("Skinning Data")]
        // The game object of a character with skinned mesh renderer and an animator
        public GameObject characterToSkin;

        // The material of this prefab. This has to be passed through as multiple instances of materials
        // can cause issues with the system
        public Material characterMat;

        private void Awake()
        {

            // Pass the 2 main components into the entity manager
            CharacterTransformData characterTransformData = new CharacterTransformData();
            AnimationData animationData = new AnimationData();

            // Get the gameobject and get the active entity manager that is created by the gamemanager
            entity = GetComponent<GameObjectEntity>().Entity;
            entityManager = World.Active.GetOrCreateManager<EntityManager>();

            if (!characterMat)
            {
                if (characterToSkin)
                    characterMat = characterToSkin.GetComponent<Renderer>().material;
            }

            // Add these componenets to the entity manager
            entityManager.AddComponentData(entity, characterTransformData);
            entityManager.AddComponentData(entity, animationData);
        }
    }
}