using UnityEngine;
using UnityEngine.EventSystems;

// shared click target for both world knobs and close-up knobs
// inspector needs:
//  1. controller reference
//  2. knob id (left/right)
// on click, asks the controller to open close-up mode

public class RadioKnobClickTarget : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private RadioController controller;
    [SerializeField] private RadioKnobId knobId;
    [SerializeField] private bool leftClickOnly = true;

    // called by the event system when object is clicked
    public void OnPointerClick(PointerEventData eventData)
    {
        // optional guard
        // right/middle click does nothing
        if (leftClickOnly && eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        // missing controller
        if (controller == null)
        {
            Debug.LogWarning($"RadioKnobClickTarget ({name}): controller is not assigned");
            return;
        }

        // open close-up
        // set this knob as active
        controller.OpenCloseUp(knobId);
    }
}
