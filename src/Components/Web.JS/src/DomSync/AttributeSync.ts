export function synchronizeAttributes(destination: Element, endState: Element) {
  // Optimize for the common case where all attributes are unchanged and are even still in the same order
  // In this case, we don't even have to build the name/value map
  const destinationAttributesLength = destination.attributes.length;
  const endStateAttributesLength = endState.attributes.length;
  let hasDifference = false;
  if (destinationAttributesLength === endStateAttributesLength) {
    for (let i = 0; i < destinationAttributesLength; i++) {
      const destAttrib = destination.attributes[i];
      const endStateAttrib = endState.attributes[i];
      if (destAttrib.name !== endStateAttrib.name || destAttrib.value !== endStateAttrib.value) {
        hasDifference = true;
        break;
      }
    }

    if (!hasDifference) {
      return;
    }
  }

  // Collect the destination attributes in a map so we can match them to the end-state attributes
  const destinationAttributesByName = new Map<string, string>();
  for (let i = 0; i < destinationAttributesLength; i++) {
    const attrib = destination.attributes[i];
    destinationAttributesByName.set(attrib.name, attrib.value);
  }

  // Loop through end state and insert/update. Track which ones we saw because any that are left
  // over will then be deleted.
  for (let i = 0; i < endStateAttributesLength; i++) {
    const endStateAttrib = endState.attributes[i];
    const endStateAttribName = endStateAttrib.name;
    const endStateAttribValue = endStateAttrib.value;
    const destinationAttribValue = destinationAttributesByName.get(endStateAttribName);
    if (destinationAttribValue !== undefined) {
      if (destinationAttribValue !== endStateAttribValue) {
        //console.log(`Changing value of attribute '${endStateAttribName}' from '${destinationAttribValue}' to '${endStateAttribValue}'`);
        destination.setAttribute(endStateAttribName, endStateAttribValue);
      }

      destinationAttributesByName.delete(endStateAttribName);
    } else {
      //console.log(`Adding attribute '${endStateAttribName}' with value '${endStateAttribValue}'`);
      destination.setAttribute(endStateAttribName, endStateAttribValue);
    }
  }

  for (let name of destinationAttributesByName.keys()) {
    //console.log(`Removing attribute '${name}' with value '${destinationAttributesByName.get(name)}'`);
    destination.removeAttribute(name);
  }
}
