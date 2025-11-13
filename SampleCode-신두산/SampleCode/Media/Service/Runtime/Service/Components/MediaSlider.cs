using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace  UCF.Media.Service
{
    internal sealed class MediaSlider : Slider
    {
        public class MediaSliderEvent : UnityEvent<eDragState, float> { }

        public new MediaSliderEvent onValueChanged = new MediaSliderEvent();

        private bool isDown = false;

        public void SetValue(float f)
        {
            if (isDown)
                return;

            value = f;
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if (!interactable)
                return;

            base.OnDrag(eventData);
            onValueChanged.Invoke(eDragState.DRAGGING, value);
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!interactable)
                return;

            base.OnPointerDown(eventData);
            isDown = true;
            onValueChanged.Invoke(eDragState.START, value);
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            if (!interactable)
                return;

            base.OnPointerUp(eventData);
            isDown = false;

            // ��ġ ��ġ�� handle ���� ���� ��쿡�� ����.
            // ��ġ ������ ��, ��ġ ��ġ�� handle�� ���� �� ��� ����Ѱɷ� �Ǵ�. �̺�Ʈ ������ ����.
            onValueChanged.Invoke(eDragState.END, value);
        }
    }
}