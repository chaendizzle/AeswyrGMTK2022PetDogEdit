using System;
using UnityEngine;

[Serializable]
public struct CharacterDialogueSpriteCollection
{
    public PetId Character;
    public GameObject Default;
    public GameObject Happy;
    public GameObject Angry;
    public AudioClip Bark;

    public GameObject Get(DogReactionType reactionType)
    {
        switch (reactionType)
        {
            case DogReactionType.Greeting:
                return Default;
            case DogReactionType.Happy:
                return Happy;
            case DogReactionType.Angry:
                return Angry;
            default:
                throw new ArgumentOutOfRangeException(nameof(reactionType), reactionType, null);
        }
    }
}