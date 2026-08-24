using System.Collections.Generic;
using WalkingIntoNight.TRPG.NPC;
using UnityEngine;

namespace WalkingIntoNight.TRPG.UI
{
    public sealed class PortraitAnimationLab : MonoBehaviour
    {
        static readonly string[] s_portraitIds =
        {
            "mei_barista_v1",
            "chen_regular_v2",
            "shop_cat_v1"
        };
        PortraitPresenter m_presenter;

        public static IReadOnlyList<string> PortraitIds => s_portraitIds;

        public void Initialize(PortraitPresenter presenter)
        {
            m_presenter = presenter;
        }

        public bool Preview(string portraitId, bool animated = true)
        {
            if (m_presenter == null) return false;
            var sprite = PortraitDatabase.Get(portraitId);
            if (sprite == null)
            {
                m_presenter.SetTalking(false);
                m_presenter.Hide(animated);
                return false;
            }

            m_presenter.Show(sprite, animated);
            m_presenter.SetTalking(true);
            return true;
        }

        public void PreviewNarrator(bool animated = true)
        {
            if (m_presenter == null) return;
            m_presenter.SetTalking(false);
            m_presenter.Hide(animated);
        }

        public void Emphasize()
        {
            m_presenter?.PlayEmotion(PortraitEmotion.Emphasis);
        }
    }
}
