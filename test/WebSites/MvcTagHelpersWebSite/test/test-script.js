
describe("<script/> tag helper", function () {
    beforeEach(function() {
      browser().navigateTo('/MvcTagHelper_Home/Script');
    });

    it("should not block page load", function() {
      expect(browser().location().path()).toBe("/MvcTagHelper_Home/Script");
    });

});