if (typeof (FKH) == "undefined") {
    FKH = { __namespace: true };
}
if (typeof (FKH.FieldAndProjectServices) == "undefined") {
    FKH.FieldAndProjectServices = {};
}
FKH.FieldAndProjectServices.JobForm = {
    jobCreated: function () {
        if (Xrm.Page.getAttribute("fkh_jobstatus") != null && Xrm.Page.getAttribute("fkh_jobstatus").getValue() != null && Xrm.Page.getAttribute("fkh_jobstatus").getValue() == 963850004/*Contract Created*/) {
            if (Xrm.Page.getAttribute("fkh_unit") != null && Xrm.Page.getAttribute("fkh_unit").getValue() != null && Xrm.Page.getAttribute("fkh_unit").getValue()[0].id != null) {
                var unitId = Xrm.Page.getAttribute("fkh_unit").getValue()[0].id;
                var req = new XMLHttpRequest();
                req.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/msdyn_projects?$select=msdyn_projectid&$filter=_fkh_unitid_value eq " + unitId + " and statuscode eq 1", true);
                req.setRequestHeader("OData-MaxVersion", "4.0");
                req.setRequestHeader("OData-Version", "4.0");
                req.setRequestHeader("Accept", "application/json");
                req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
                req.setRequestHeader("Prefer", "odata.include-annotations=\"*\"");
                req.onreadystatechange = function() {
                    if (this.readyState === 4) {
                        req.onreadystatechange = null;
                        if (this.status === 200) {
                            FKH.FieldAndProjectServices.JobForm.processProjects(JSON.parse(this.response));
                        } else {
                            Xrm.Utility.alertDialog("Problem retrieving active Project for this Unit.  Please inform the CRM Help Desk: " + this.statusText);
                        }
                    }
                };
                req.send();
            }
            else {
                Xrm.Utility.alertDialog("A unit doesn't not appear to be related to this job, so no Project will be updated.  Please inform the CRM Help Desk.");
            }
        }
    },

    processProjects: function (results) {
        for (var i = 0; i < results.value.length; i++) {
            var projectId = results.value[i]["msdyn_projectid"];
            FKH.FieldAndProjectServices.JobForm.getTasks(projectId, ['Job Assignment to Vendor(s) in Contract Creator','Job and Contract(s) Submitted to Yardi']);
        }
    },

    getTasks: function(projectId, targetTaskNames){
        var targetTaskNameFilter = "msdyn_subject eq '" + targetTaskNames[0] + "'";
        if (targetTaskNames.length > 1) {
            for (i = 1; i < targetTaskNames.length; i++) { 
                targetTaskNameFilter += " or msdyn_subject eq '" + targetTaskNames[i] + "'";
            }
        }
        var req = new XMLHttpRequest();
        req.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/msdyn_projecttasks?$select=msdyn_actualend,msdyn_actualstart,msdyn_projecttaskid,msdyn_scheduledend,msdyn_scheduledstart,msdyn_subject,statuscode&$filter=statuscode ne 963850001 and _msdyn_project_value eq " + projectId + " and (" + targetTaskNameFilter + ")", true);
        req.setRequestHeader("OData-MaxVersion", "4.0");
        req.setRequestHeader("OData-Version", "4.0");
        req.setRequestHeader("Accept", "application/json");
        req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
        req.setRequestHeader("Prefer", "odata.include-annotations=\"*\"");
        req.onreadystatechange = function() {
            if (this.readyState === 4) {
                req.onreadystatechange = null;
                if (this.status === 200) {
                    FKH.FieldAndProjectServices.JobForm.processTasks(JSON.parse(this.response));
                } else {
                    Xrm.Utility.alertDialog(this.statusText);
                }
            }
        };
        req.send();
    },

    processTasks: function(results){
        for (var i = 0; i < results.value.length; i++) {
            switch(results.value[i]['msdyn_subject']) {
                case 'Job Assignment to Vendor(s) in Contract Creator':
                case 'Job and Contract(s) Submitted to Yardi':
                    FKH.FieldAndProjectServices.JobForm.completeTask(results.value[i]);
                    break;
                default:
            }
        }
    },

    completeTask: function(task) {
        if (task["statuscode"] != 963850001) { //Completed
            var entity = {};
            if (task["msdyn_actualstart"] == null) {
                entity.msdyn_actualstart = new Date().toISOString();
            }
            entity.msdyn_actualend = new Date().toISOString();
            entity.msdyn_progress = 100;
            entity.statuscode = 963850001; //Completed

            var req = new XMLHttpRequest();
            req.open("PATCH", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/msdyn_projecttasks("+ task["msdyn_projecttaskid"] +")", true);
            req.setRequestHeader("OData-MaxVersion", "4.0");
            req.setRequestHeader("OData-Version", "4.0");
            req.setRequestHeader("Accept", "application/json");
            req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
            req.onreadystatechange = function() {
                if (this.readyState === 4) {
                    req.onreadystatechange = null;
                    if (this.status === 204) {
                        //Success
                    } else {
                        Xrm.Utility.alertDialog("The Budget Start task may not have updated properly.  Please inform the CRM Help Desk. (" + this.statusText + ")");
                    }
                }
            };
            req.send(JSON.stringify(entity));
        }
    }
}