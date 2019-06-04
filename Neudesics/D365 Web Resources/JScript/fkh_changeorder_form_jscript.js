function OnProjectChange() {
    var project = Xrm.Page.getAttribute("fkh_projectid").getValue();
    if (project !== null && project !== undefined) {
        Xrm.WebApi.online.retrieveRecord("msdyn_project", project[0].id.replace('{', '').replace('}', ''), "?$select=msdyn_subject,_msdyn_projecttemplate_value").then(
            function success(result) {
                var msdyn_subject = result["msdyn_subject"];
                if (msdyn_subject !== null && msdyn_subject !== undefined && msdyn_subject !== "") {
                    Xrm.Page.getAttribute('fkh_name').setValue("CO for " + msdyn_subject);
                    Xrm.Page.getAttribute("fkh_name").setSubmitMode("always");
                }
                var _msdyn_projecttemplate_value = result["_msdyn_projecttemplate_value"];
                var _msdyn_projecttemplate_value_formatted = result["_msdyn_projecttemplate_value@OData.Community.Display.V1.FormattedValue"];
                var _msdyn_projecttemplate_value_lookuplogicalname = result["_msdyn_projecttemplate_value@Microsoft.Dynamics.CRM.lookuplogicalname"];

                if (_msdyn_projecttemplate_value !== null && _msdyn_projecttemplate_value !== undefined) {
                    Xrm.Page.getAttribute("fkh_projecttemplateid").setValue([{ id: _msdyn_projecttemplate_value, name: _msdyn_projecttemplate_value_formatted, entityType: _msdyn_projecttemplate_value_lookuplogicalname }]);
                }

            },
            function (error) {
                Xrm.Utility.alertDialog(error.message);
            }
        );
    }
}

function onLoad() {
    var formType = Xrm.Page.ui.getFormType();
    if (formType === 1) {
        OnProjectChange();
    }
}