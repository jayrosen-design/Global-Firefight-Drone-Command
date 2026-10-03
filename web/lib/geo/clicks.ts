/** Pixels the pointer may move between down and up for a globe click to count (larger = a drag/orbit). */
export const CLICK_DRAG_TOLERANCE_PX = 5;
export const isDrag = (e: { delta: number }) => e.delta > CLICK_DRAG_TOLERANCE_PX;
