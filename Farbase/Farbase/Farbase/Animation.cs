using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Farbase
{
    public interface IAnimateable
    {
        Animateable GetAnimateable();
        AnimationValues GetAnimationValues();
    }

    public class Animateable
    {
        private static List<Animateable> animateables
            = new List<Animateable>();

        public static void UpdateAll(int ms)
        {
            foreach (Animateable a in animateables)
                a.Update(ms);
        }

        private List<Animation> animations;
        private IAnimateable provider;

        public Animateable(IAnimateable provider)
        {
            animations = new List<Animation>();
            this.provider = provider;

            //autoregister
            animateables.Add(this);
        }

        public AnimationValues ApplyAnimations()
        {
            AnimationValues currentValues = provider.GetAnimationValues();
            foreach (Animation a in animations)
                currentValues = a.ApplyTo(currentValues);
            return currentValues;
        }

        public void AddAnimation(Animation a)
        {
            animations.Add(a);
        }

        public void Update(int ms)
        {
            foreach (Animation a in animations)
                a.Update(ms);

            animations = animations
                .Where(a => !a.Finished)
                .ToList();
        }

        public void Destroy()
        {
            animateables.Remove(this);
        }
    }

    public enum CurveType
    {
        Linear,
        EaseIn,
        EaseOut,
        Smooth,
        Twist
    }

    public abstract class Animation
    {
        public int Length, Remaining; //ms
        public CurveType CurveType;

        //higher => "harder" smooth curves
        private const float exponentialCurvePower = 1.5f;

        protected Animation(
            int length,
            CurveType curveType
        ) {
            Length = Remaining = length;
            CurveType = curveType;
        }

        public void Update(int ms)
        {
            Remaining -= ms;
            Remaining = Math.Max(0, Remaining);
        }

        public float GetCurveValue()
        {
            float x = (float)(Length - Remaining) / Length;

            switch (CurveType)
            {
                case CurveType.Linear:
                    return GetCurveValue(x, CurveType.Linear);

                case CurveType.EaseIn:
                    return GetCurveValue(x, CurveType.EaseIn);

                case CurveType.EaseOut:
                    return GetCurveValue(x, CurveType.EaseOut);

                case CurveType.Smooth:
                    if (x < 0.5f)
                        return GetCurveValue(
                            x * 2,
                            CurveType.EaseIn
                        ) / 2f;
                    return 0.5f + GetCurveValue(
                        (x - 0.5f) * 2,
                        CurveType.EaseOut
                    ) / 2f;

                case CurveType.Twist:
                    if (x < 0.5f)
                        return GetCurveValue(
                            x * 2,
                            CurveType.EaseOut
                        ) / 2f;
                    return 0.5f + GetCurveValue(
                        (x - 0.5f) * 2,
                        CurveType.EaseIn
                    ) / 2f;

                default:
                    throw new ArgumentException();
            }
        }

        private float GetCurveValue(float x, CurveType curveType)
        {
            switch (curveType)
            {
                case CurveType.Linear:
                    return x;

                case CurveType.EaseIn:
                    return (float)Math.Pow(x, exponentialCurvePower);

                case CurveType.EaseOut:
                    return 1 - (float)Math.Pow((1 - x), exponentialCurvePower);

                default:
                    throw new ArgumentException();
            }
        }

        public abstract AnimationValues ApplyTo(AnimationValues a);

        public bool Finished
        {
            get { return Remaining == 0; }
        }
    }

    //Collection of animateable values provided by an IAnimateable,
    //which is then passed through the chain of Animations on the target
    //to produce the final values.
    public class AnimationValues
    {
        public Vector2 Position;
        //public Color Color?

        public AnimationValues(
            Vector2 position
        ) {
            Position = position;
        }
    }

    //notice, currently only the movement animation and nothing else
    //mainly because the rest of the animations I can come up with atm are
    //either animations of the sprite itself (which we need to handle
    //differently, I guess? maybe not) or particle effects.
    //doing it like this for the sake of the future, think of the children.

    public class PositionAnimation : Animation
    {
        public Vector2 Source, Target;

        public PositionAnimation(
            int length,
            CurveType curveType,
            Vector2 source,
            Vector2 target
        ) : base(length, curveType) {
            Source = source;
            Target = target;
        }

        public override AnimationValues ApplyTo(AnimationValues a)
        {
            //NOTICE ORDER!
            //Usually you would do Target - Source,
            //but we're not actually moving towards target,
            //the animation is sort of an... Unmove.
            //Codewise, the animatee is already at the targetposition,
            //We're just "slowing down" the visual movement there by
            //"unmoving" it, and then "unmoving" it less as time goes on.

            Vector2 fullDelta = Source - Target;
            //Above reason is also why we SUBTRACT the curve value from 1,
            //instead of using it directly.
            //CurveValue goes from 0 -> 1, and we need to apply from
            //full "unmove" (1) to none (0), so we reverse it with 1f - y.
            Vector2 apply = fullDelta * (1f - GetCurveValue());
                //((float)Remaining / Length);
            a.Position += apply;
            return a;
        }
    }
}