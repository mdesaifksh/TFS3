if (typeof (FKH) == "undefined") {
    FKH = { __namespace: true };
}
if (typeof (FKH.FieldAndProjectServices) == "undefined") {
    FKH.FieldAndProjectServices = {};
}
FKH.FieldAndProjectServices.ProjectForm = {
    onFormLoad: function () {
        var template = Xrm.Page.getAttribute("msdyn_projecttemplate").getValue();
        if (template != null && template[0] != null && template[0].name != null && template[0].name == "Turn Process") {
            Xrm.Page.getAttribute("fkh_reasonformoveout").setVisible(true);
        } else {
            Xrm.Page.getAttribute("fkh_reasonformoveout").setVisible(false);
        }
    },

    onChange_ReasonForMoveOut: function () {
        var reasonForMoveOut = Xrm.Page.getAttribute("fkh_reasonformoveout").getValue();
        var projectId = Xrm.Page.data.entity.getId().toString().replace("{","").replace("}","");
        switch (reasonForMoveOut) {
            case 963850000: //Scheduled Move-Out
                break;
            case 963850001: //Eviction
            case 963850002: //Resident Skip
                FKH.FieldAndProjectServices.ProjectForm.completeTask("Corporate Renewals",projectId);
                FKH.FieldAndProjectServices.ProjectForm.completeTask("Market Schedules Pre-Move-Out",projectId);
                FKH.FieldAndProjectServices.ProjectForm.completeTask("Pre-Move-Out Inspection",projectId);
                break;
            default:
        }
    },

    completeTask: function (taskName,projectId) {
        var getReq = new XMLHttpRequest();
        getReq.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/msdyn_projecttasks?$select=msdyn_actualstart&$filter=_msdyn_project_value eq "+projectId+" and  msdyn_subject eq '"+taskName+"' and  statuscode ne 963850001 and statuscode ne 2", true);
        getReq.setRequestHeader("OData-MaxVersion", "4.0");
        getReq.setRequestHeader("OData-Version", "4.0");
        getReq.setRequestHeader("Accept", "application/json");
        getReq.setRequestHeader("Content-Type", "application/json; charset=utf-8");
        getReq.onreadystatechange = function() {
            if (this.readyState === 4) {
                getReq.onreadystatechange = null;
                if (this.status === 200) {
                    var results = JSON.parse(this.response);
                    for (var i = 0; i < results.value.length; i++) {
                        var entity = {};
                        var startDate;
                        if (results.value[i]["msdyn_actualstart"] == null) {
                            entity.msdyn_actualstart = new Date().toISOString();
                            startDate = new Date();
                        } else {
                            startDate = new Date(results.value[i]["msdyn_actualstart"]);
                        }
                        entity.msdyn_actualend = new Date().toISOString();
                        endDate = new Date();
                        var minutesBetween = FKH.FieldAndProjectServices.ProjectForm.dateDiffInMinutes(startDate,endDate);
                        try{
                            entity.msdyn_actualdurationminutes = minutesBetween;
                        } catch (err) {
                            Xrm.Utility.alertDialog(err);
                        }
                        entity.msdyn_progress = 100;
                        entity.statuscode = 963850001; //Completed
            
                        var updateReq = new XMLHttpRequest();
                        updateReq.open("PATCH", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/msdyn_projecttasks(" + results.value[i]["msdyn_projecttaskid"] + ")", true);
                        updateReq.setRequestHeader("OData-MaxVersion", "4.0");
                        updateReq.setRequestHeader("OData-Version", "4.0");
                        updateReq.setRequestHeader("Accept", "application/json");
                        updateReq.setRequestHeader("Content-Type", "application/json; charset=utf-8");
                        updateReq.onreadystatechange = function () {
                            if (this.readyState === 4) {
                                updateReq.onreadystatechange = null;
                                if (this.status === 204) {
                                    //Success
                                } else {
                                    Xrm.Utility.alertDialog("The '" + taskName + "' task may not have updated properly.  Please inform the CRM Help Desk. (" + this.statusText + ")");
                                }
                            }
                        };
                        updateReq.send(JSON.stringify(entity));
                    }
                } else {
                    Xrm.Utility.alertDialog(this.statusText);
                }
            }
        };
        getReq.send();
    },

    dateDiffInMinutes: function (startDate,endDate) {
        return Math.floor((endDate - startDate) / (1000*60));
    }
}
