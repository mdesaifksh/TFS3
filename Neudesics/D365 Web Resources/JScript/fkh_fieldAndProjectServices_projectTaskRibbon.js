if (typeof (FKH) == "undefined") {
    FKH = { __namespace: true };
}
if (typeof (FKH.FieldAndProjectServices) == "undefined") {
    FKH.FieldAndProjectServices = {};
}
FKH.FieldAndProjectServices.ProjectTaskRibbon = {

    onClick_MarkComplete: function () {
        if (Xrm.Page.getAttribute("msdyn_subject") != null && Xrm.Page.getAttribute("msdyn_subject").getValue() != null){
            var thisTaskName = Xrm.Page.getAttribute("msdyn_subject").getValue();
            switch(thisTaskName) {
                case 'Pre-Move-Out Inspection':
                    Xrm.Page.getAttribute("msdyn_actualend").setValue(new Date());
                    Xrm.Page.getAttribute("msdyn_progress").setValue(100);
                    Xrm.Page.getAttribute("statuscode").setValue(963850001);//Completed
                    Xrm.Page.data.refresh(true);
                case 'Move-Out Inspection':
                    Xrm.Page.getAttribute("msdyn_actualstart").setValue(new Date());
                    Xrm.Page.getAttribute("msdyn_actualend").setValue(new Date());
                    Xrm.Page.getAttribute("msdyn_progress").setValue(100);
                    Xrm.Page.getAttribute("statuscode").setValue(963850001);//Completed
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.getTasks(thisTaskName, Xrm.Page.getAttribute("msdyn_project").getValue()[0].id, ['Budget Start'])
                    Xrm.Page.data.refresh(true);
                case 'Budget Start':
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.getTasks(thisTaskName, Xrm.Page.getAttribute("msdyn_project").getValue()[0].id, ['Work In Progress','Quality Control Inspection','Hero Shot Picture'])
                case 'Budget Approval':
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.getTasks(thisTaskName, Xrm.Page.getAttribute("msdyn_project").getValue()[0].id, ['Work In Progress','Quality Control Inspection','Hero Shot Picture'])
                case 'Vendor(s) Says Job Started':
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.getTasks(thisTaskName, Xrm.Page.getAttribute("msdyn_project").getValue()[0].id, ['Work In Progress','Quality Control Inspection','Hero Shot Picture'])
                case 'Work In Progress':
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.getTasks(thisTaskName, Xrm.Page.getAttribute("msdyn_project").getValue()[0].id, ['Work In Progress','Quality Control Inspection','Hero Shot Picture'])
                case 'Quality Control Inspection':
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.getTasks(thisTaskName, Xrm.Page.getAttribute("msdyn_project").getValue()[0].id, ['Work In Progress','Quality Control Inspection','Hero Shot Picture'])
                case 'Hero Shot Picture':
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.getTasks(thisTaskName, Xrm.Page.getAttribute("msdyn_project").getValue()[0].id, ['Work In Progress','Quality Control Inspection','Hero Shot Picture'])
                case 'Marketing Inspection':
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.getTasks(thisTaskName, Xrm.Page.getAttribute("msdyn_project").getValue()[0].id, ['Work In Progress','Quality Control Inspection','Hero Shot Picture'])
                case 'Bi-Weekly Inspection':
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.getTasks(thisTaskName, Xrm.Page.getAttribute("msdyn_project").getValue()[0].id, ['Work In Progress','Quality Control Inspection','Hero Shot Picture'])
                case 'Move-In Inspection':
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.getTasks(thisTaskName, Xrm.Page.getAttribute("msdyn_project").getValue()[0].id, ['Work In Progress','Quality Control Inspection','Hero Shot Picture'])
                default:
            }
        }
        return false;
    },
    
    isVisible_MarkComplete: function () {
        if (Xrm.Page.getAttribute("msdyn_subject") != null && Xrm.Page.getAttribute("msdyn_subject").getValue() != null){
            var taskName = Xrm.Page.getAttribute("msdyn_subject").getValue();
            var taskStatus = Xrm.Page.getAttribute("statuscode").getValue();
            if (taskStatus != 963850001) {
                switch(taskName) {
                    case 'Pre-Move-Out Inspection':
                    case 'Move-Out Inspection':
                    case 'Budget Start':
                    case 'Budget Approval':
                    case 'Vendor(s) Says Job Started':
                    case 'Work In Progress':
                    case 'Quality Control Inspection':
                    case 'Hero Shot Picture':
                    case 'Marketing Inspection':
                    case 'Bi-Weekly Inspection':
                    case 'Move-In Inspection':
                        return true;
                    default:
                        return false;
                }
            }
        }
        return false;
    },

    getTasks: function(thisTaskName, projectId, targetTaskNames){
        var targetTaskNameFilter = "msdyn_subject eq '" + targetTaskNames[0] + "'";
        if (targetTaskNames.length > 1) {
            for (i = 1; i < targetTaskNames.length; i++) { 
                targetTaskNameFilter += " or msdyn_subject eq '" + targetTaskNames[i] + "'";
            }
        }
        var req = new XMLHttpRequest();
        req.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/msdyn_projecttasks?$select=msdyn_actualend,msdyn_actualstart,msdyn_projecttaskid,msdyn_scheduledend,msdyn_scheduledstart,msdyn_subject,statuscode&$filter=_msdyn_project_value eq " + projectId + " and (" + targetTaskNameFilter + ")", true);
        req.setRequestHeader("OData-MaxVersion", "4.0");
        req.setRequestHeader("OData-Version", "4.0");
        req.setRequestHeader("Accept", "application/json");
        req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
        req.setRequestHeader("Prefer", "odata.include-annotations=\"*\"");
        req.onreadystatechange = function() {
            if (this.readyState === 4) {
                req.onreadystatechange = null;
                if (this.status === 200) {
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.processTasks(thisTaskName, JSON.parse(this.response));
                } else {
                    Xrm.Utility.alertDialog(this.statusText);
                }
            }
        };
        req.send();
    },

    processTasks: function(thisTaskName, results){
        switch(thisTaskName) {
            case 'Move-Out Inspection':
                FKH.FieldAndProjectServices.ProjectTaskRibbon.startBudgetStart(thisTaskName, results);
            case 'Budget Start':
            case 'Budget Approval':
            case 'Vendor(s) Says Job Started':
            case 'Work In Progress':
            case 'Quality Control Inspection':
            case 'Hero Shot Picture':
            case 'Marketing Inspection':
            case 'Bi-Weekly Inspection':
            case 'Move-In Inspection':
                return true;
            default:
                return false;
        }
        for (var i = 0; i < results.value.length; i++) {
            var msdyn_actualend = results.value[i]["msdyn_actualend"];
            var msdyn_actualstart = results.value[i]["msdyn_actualstart"];
            var msdyn_projecttaskid = results.value[i]["msdyn_projecttaskid"];
            var msdyn_scheduledend = results.value[i]["msdyn_scheduledend"];
            var msdyn_scheduledstart = results.value[i]["msdyn_scheduledstart"];
            var msdyn_subject = results.value[i]["msdyn_subject"];
            var statuscode = results.value[i]["statuscode"];
            Xrm.Utility.alertDialog(thisTaskName + " - " + msdyn_subject);
        }
    },

    startBudgetStart: function(thisTaskName, results) {
        var entity = {};
        entity.msdyn_actualstart = new Date().toISOString();
        entity.msdyn_progress = 1;
        entity.statuscode = 963850000; //In Progress

        var req = new XMLHttpRequest();
        req.open("PATCH", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/msdyn_projecttasks("+ results.value[0]["msdyn_projecttaskid"] +")", true);
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
    },
    
    testOutgoingIntegration: function () {
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
    }
}