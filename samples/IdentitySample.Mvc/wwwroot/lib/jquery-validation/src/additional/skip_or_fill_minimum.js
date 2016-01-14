/*
 * Lets you say "either at least X inputs that match selector Y must be filled,
 * OR they must all be skipped (left blank)."
 *
 * The end result, is that none of these inputs:
 *
 *	<input class="productinfo" name="partnumber">
 *	<input class="productinfo" name="description">
 *	<input class="productinfo" name="color">
 *
 *	...will validate unless either at least two of them are filled,
 *	OR none of them are.
 *
 * partnumber:	{skip_or_fill_minimum: [2,".productinfo"]},
 * description: {skip_or_fill_minimum: [2,".productinfo"]},
 * color:		{skip_or_fill_minimum: [2,".productinfo"]}
 *
 * options[0]: number of fields that must be filled in the group
 * options[1]: CSS selector that defines the group of conditionally required fields
 *
 */
$.validator.addMethod("skip_or_fill_minimum", function(value, element, options) {
	var $fields = $(options[1], element.form),
		$fieldsFirst = $fields.eq(0),
		validator = $fieldsFirst.data("valid_skip") ? $fieldsFirst.data("valid_skip") : $.extend({}, this),
		numberFilled = $fields.filter(function() {
			return validator.elementValue(this);
		}).length,
		isValid = numberFilled === 0 || numberFilled >= options[0];

	// Store the cloned validator for future validation
	$fieldsFirst.data("valid_skip", validator);

	// If element isn't being validated, run each skip_or_fill_minimum field's validation rules
	if (!$(element).data("being_validated")) {
		$fields.data("being_validated", true);
		$fields.each(function() {
			validator.element(this);
		});
		$fields.data("being_validated", false);
	}
	return isValid;
}, $.validator.format("Please either skip these fields or fill at least {0} of them."));
