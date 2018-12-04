if (typeof (FKH) == "undefined") {
    FKH = { __namespace: true };
}
if (typeof (FKH.FieldAndProjectServices) == "undefined") {
    FKH.FieldAndProjectServices = {};
}
FKH.FieldAndProjectServices.ProjectTaskRibbon = {

    onClick_WorkCompleted: function () {
        var entity = {};
        entity.fkh_direction = true;
        entity.fkh_eventdata = "[{   'Id': '<need to define>',   'EventType': '<need to define>',   'Subject': 'Dynamics: Job Complete',   'EventTime': '<need to define>',   'Data': {     'Property': '<need to define>',     'Job': '<need to define>',     'Contract': '<need to define>',     'Event': 'Job Complete'   },   'dataVersion': '',   'metadataVersion': '1',   'Topic': '<need to define>' }]";
        entity.fkh_name = "Dynamics: Job Complete";

        var req = new XMLHttpRequest();
        req.open("POST", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/fkh_azureintegrationcalls", true);
        req.setRequestHeader("OData-MaxVersion", "4.0");
        req.setRequestHeader("OData-Version", "4.0");
        req.setRequestHeader("Accept", "application/json");
        req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
        req.onreadystatechange = function() {
            if (this.readyState === 4) {
                req.onreadystatechange = null;
                if (this.status === 204) {
                    var uri = this.getResponseHeader("OData-EntityId");
                    var regExp = /\(([^)]+)\)/;
                    var matches = regExp.exec(uri);
                    var newEntityId = matches[1];
                } else {
                    Xrm.Utility.alertDialog(this.statusText);
                }
            } else {
            }
        };
        Xrm.Utility.alertDialog("Marking vendor's work on this job as complete in all systems.");
        req.send(JSON.stringify(entity));
    },
    
    isVisible_WorkCompleted: function () {
        if (Xrm.Page.getAttribute("msdyn_subject") != null){
            if (Xrm.Page.getAttribute("msdyn_subject").getValue() != null && Xrm.Page.getAttribute("msdyn_subject").getValue() == 'Work in progress') {
                return true;
            }
        }
        return false;
    }
}