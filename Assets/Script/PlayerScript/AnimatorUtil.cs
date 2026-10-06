using UnityEngine;

/// <summary>
/// Safe Animator helpers used by Shooting and Skill_rail.
/// If the Animator is empty, or doesn't have a parameter with that name (yet),
/// the call is silently skipped instead of spamming warnings.
/// That way scripts work BEFORE the animators are finished.
/// Not a component — don't attach it to anything.
/// </summary>
public static class AnimatorUtil
{
    public static bool HasParam(Animator anim, string name, AnimatorControllerParameterType type)
    {
        if (anim == null || string.IsNullOrEmpty(name) || anim.runtimeAnimatorController == null) return false;
        foreach (AnimatorControllerParameter p in anim.parameters)
            if (p.type == type && p.name == name) return true;
        return false;
    }

    public static void Trigger(Animator anim, string name)
    {
        if (HasParam(anim, name, AnimatorControllerParameterType.Trigger)) anim.SetTrigger(name);
    }

    public static void ResetTrigger(Animator anim, string name)
    {
        if (HasParam(anim, name, AnimatorControllerParameterType.Trigger)) anim.ResetTrigger(name);
    }

    public static void Bool(Animator anim, string name, bool value)
    {
        if (HasParam(anim, name, AnimatorControllerParameterType.Bool)) anim.SetBool(name, value);
    }

    public static void Float(Animator anim, string name, float value)
    {
        if (HasParam(anim, name, AnimatorControllerParameterType.Float)) anim.SetFloat(name, value);
    }
}
