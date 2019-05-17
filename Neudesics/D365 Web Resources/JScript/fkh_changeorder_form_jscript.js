function OnProjectChange() {
    var project = Xrm.Page.getAttribute("fkh_projectid").getValue();
    if (project !== null && project !== undefined) {
        Xrm.WebApi.online.retrieveRecord("msdyn_project", project[0].id.replace('{', '').replace('}', ''), "?$select=msdyn_subject").then(
            function success(result) {
                var msdyn_subject = result["msdyn_subject"];
                if (msdyn_subject !== null && msdyn_subject !== undefined && msdyn_subject !== "") {
                    Xrm.Page.getAttribute('fkh_name').setValue("CO for " + msdyn_subject);
                    Xrm.Page.getAttribute("fkh_name").setSubmitMode("always");
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