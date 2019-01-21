if (typeof (FKH) == "undefined") {
    FKH = { __namespace: true };
}
if (typeof (FKH.FieldAndProjectServices) == "undefined") {
    FKH.FieldAndProjectServices = {};
}
FKH.FieldAndProjectServices.ProjectTaskRibbon = {

    onClick_MarkComplete: function () {
        if (Xrm.Page.getAttribute("msdyn_subject") != null && Xrm.Page.getAttribute("msdyn_subject").getValue() != null) {
            var thisTaskName = Xrm.Page.getAttribute("msdyn_subject").getValue();
            var thisProjectId = Xrm.Page.getAttribute("msdyn_project").getValue()[0].id;
            switch (thisTaskName) {
                case 'Pre-Move-Out Inspection':
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.completeThisTask();
                    Xrm.Page.data.refresh(true);
                    break;
                case 'Move-Out Inspection':
                    Xrm.Page.getAttribute("msdyn_actualstart").setValue(new Date());
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.completeThisTask();
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.getTasks(thisTaskName, thisProjectId, ['Budget Start']);
                    Xrm.Page.data.refresh(true);
                    break;
                case 'Budget Start':
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.completeThisTask();
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.getTasks(thisTaskName, thisProjectId, ['Budget Approval']);
                    Xrm.Page.data.refresh(true);
                    break;
                case 'Budget Approval':
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.completeThisTask();
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.getTasks(thisTaskName, thisProjectId, ['Job Assignment to Vendor(s) in Contract Creator']);
                    Xrm.Page.data.refresh(true);
                    break;
                case 'Vendor(s) Says Job Started':
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.completeThisTask();
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.getTasks(thisTaskName, thisProjectId, ['Work in Progress']);
                    Xrm.Page.data.refresh(true);
                    break;
                case 'Work In Progress':
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.completeThisTask();
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.getTasks(thisTaskName, thisProjectId, ['Vendor Says Job\'s Complete', 'Quality Control Inspection']);
                    Xrm.Page.data.refresh(true);
                    break;
                case 'Vendor Says Job\'s Complete':
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.completeThisTask();
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.getTasks(thisTaskName, thisProjectId, ['Work In Progress', 'Quality Control Inspection']);
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.publishMessage(thisTaskName);
                    Xrm.Page.data.refresh(true);
                    break;
                case 'Quality Control Inspection':
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.completeThisTask();
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.createReWorkTasks(thisProjectId);
                    Xrm.Page.data.refresh(true);
                    break;
                case 'Job Completed':
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.completeThisTask();
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.getTasks(thisTaskName, thisProjectId, ['Marketing Inspection', 'Quality Control Inspection']);
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.publishMessage(thisTaskName);
                    Xrm.Page.data.refresh(true);
                    break;
                case 'Hero Shot Picture':
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.completeThisTask();
                    Xrm.Page.data.refresh(true);
                    break;
                case 'Marketing Inspection':
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.completeThisTask();
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.getTasks(thisTaskName, Xrm.Page.getAttribute("msdyn_project").getValue()[0].id, ['Bi-Weekly Inspection']);
                    Xrm.Page.data.refresh(true);
                    break;
                case 'Bi-Weekly Inspection':
                    //FKH.FieldAndProjectServices.ProjectTaskRibbon.getTasks(thisTaskName, Xrm.Page.getAttribute("msdyn_project").getValue()[0].id, ['Work In Progress']);
                    break;
                case 'Move-In Inspection':
                    //FKH.FieldAndProjectServices.ProjectTaskRibbon.getTasks(thisTaskName, Xrm.Page.getAttribute("msdyn_project").getValue()[0].id, ['Work In Progress']);
                    break;
                default:
            }
        }
        return false;
    },

    completeThisTask: function () {
        if (Xrm.Page.getAttribute("msdyn_actualstart").getValue() == null) {
            Xrm.Page.getAttribute("msdyn_actualstart").setValue(new Date());
        }
        Xrm.Page.getAttribute("msdyn_actualend").setValue(new Date());
        Xrm.Page.getAttribute("msdyn_progress").setValue(100);
        Xrm.Page.getAttribute("statuscode").setValue(963850001);//Completed
    },

    isVisible_MarkComplete: function () {
        if (Xrm.Page.getAttribute("msdyn_subject") != null && Xrm.Page.getAttribute("msdyn_subject").getValue() != null) {
            var taskName = Xrm.Page.getAttribute("msdyn_subject").getValue();
            var taskStatus = Xrm.Page.getAttribute("statuscode").getValue();
            if (taskStatus != 963850001) {//Completed
                switch (taskName) {
                    case 'Pre-Move-Out Inspection':
                    case 'Move-Out Inspection':
                    case 'Budget Start':
                    case 'Budget Approval':
                    case 'Vendor(s) Says Job Started':
                    case 'Vendor Says Job\'s Complete':
                    case 'Quality Control Inspection':
                    case 'Job Completed':
                    case 'Hero Shot Picture':
                    case 'Marketing Inspection':
                    case 'Bi-Weekly Inspection':
                    case 'Move-In Inspection':
                    case 'Job Completed':
                        return true;
                    default:
                        return false;
                }
            }
        }
        return false;
    },

    getTasks: function (thisTaskName, projectId, targetTaskNames) {
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
        req.onreadystatechange = function () {
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

    processTasks: function (thisTaskName, results) {
        switch (thisTaskName) {
            case 'Move-Out Inspection':
            case 'Budget Start':
            case 'Budget Approval':
            case 'Vendor(s) Says Job Started':
                FKH.FieldAndProjectServices.ProjectTaskRibbon.startTask(results.value[0]);
                break;
            case 'Work In Progress':
                for (var i = 0; i < results.value.length; i++) {
                    switch (results.value[i]["msdyn_subject"]) {
                        case 'Vendor Says Job\'s Complete':
                            FKH.FieldAndProjectServices.ProjectTaskRibbon.completeTask(results.value[i]);
                        case 'Quality Control Inspection':
                            FKH.FieldAndProjectServices.ProjectTaskRibbon.startTask(results.value[i]);
                        default:
                    }
                }
                break;
            case 'Vendor Says Job\'s Complete':
                for (var i = 0; i < results.value.length; i++) {
                    switch (results.value[i]["msdyn_subject"]) {
                        case 'Quality Control Inspection':
                            FKH.FieldAndProjectServices.ProjectTaskRibbon.startTask(results.value[i]);
                        case 'Work In Progress':
                            FKH.FieldAndProjectServices.ProjectTaskRibbon.completeTask(results.value[i]);
                        default:
                    }
                }
                break;
            case 'Job Completed':
                for (var i = 0; i < results.value.length; i++) {
                    switch (results.value[i]["msdyn_subject"]) {
                        case 'Quality Control Inspection':
                            FKH.FieldAndProjectServices.ProjectTaskRibbon.completeTask(results.value[i]);
                        case 'Marketing Inspection':
                            FKH.FieldAndProjectServices.ProjectTaskRibbon.startTask(results.value[i]);
                        default:
                    }
                }
                break;
            case 'Marketing Inspection':
                FKH.FieldAndProjectServices.ProjectTaskRibbon.scheduleBiWeeklyInspection();
                break;
            case 'Bi-Weekly Inspection':
            case 'Move-In Inspection':
            default:
        }
    },

    startTask: function (task) {
        if (task["statuscode"] == 1) { //Not Started
            var entity = {};
            entity.msdyn_actualstart = new Date().toISOString();
            entity.msdyn_progress = 1;
            entity.statuscode = 963850000; //In Progress

            var req = new XMLHttpRequest();
            req.open("PATCH", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/msdyn_projecttasks(" + task["msdyn_projecttaskid"] + ")", true);
            req.setRequestHeader("OData-MaxVersion", "4.0");
            req.setRequestHeader("OData-Version", "4.0");
            req.setRequestHeader("Accept", "application/json");
            req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
            req.onreadystatechange = function () {
                if (this.readyState === 4) {
                    req.onreadystatechange = null;
                    if (this.status === 204) {
                        //Success
                    } else {
                        Xrm.Utility.alertDialog("The '" + task["msdyn_subject"] + "' task may not have updated properly.  Please inform the CRM Help Desk. (" + this.statusText + ")");
                    }
                }
            };
            req.send(JSON.stringify(entity));
        }
    },

    completeTask: function (task) {
        if (task["statuscode"] != 963850001) { //Completed
            var entity = {};
            if (task["msdyn_actualstart"] == null) {
                entity.msdyn_actualstart = new Date().toISOString();
            }
            entity.msdyn_actualend = new Date().toISOString();
            entity.msdyn_progress = 100;
            entity.statuscode = 963850001; //Completed

            var req = new XMLHttpRequest();
            req.open("PATCH", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/msdyn_projecttasks(" + task["msdyn_projecttaskid"] + ")", true);
            req.setRequestHeader("OData-MaxVersion", "4.0");
            req.setRequestHeader("OData-Version", "4.0");
            req.setRequestHeader("Accept", "application/json");
            req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
            req.onreadystatechange = function () {
                if (this.readyState === 4) {
                    req.onreadystatechange = null;
                    if (this.status === 204) {
                        //Success
                    } else {
                        Xrm.Utility.alertDialog("The '" + task["msdyn_subject"] + "' task may not have updated properly.  Please inform the CRM Help Desk. (" + this.statusText + ")");
                    }
                }
            };
            req.send(JSON.stringify(entity));
        }
    },

    createReWorkTasks: function (projectId) {

    },

    publishMessage: function (thisTaskName) {

        //alert("Publish Message");
        var unit = Xrm.Page.getAttribute('fkh_unitid').getValue();
        var taskIdentifier = Xrm.Page.getAttribute('fkh_taskidentifierid').getValue();
        var today = new Date();
        var date = today.getFullYear() + '-' + (today.getMonth() + 1) + '-' + today.getDate();
        var time = today.getHours() + ":" + today.getMinutes() + ":" + today.getSeconds();
        var dateTime = date + ' ' + time;

        debugger;
        if (unit !== null && taskIdentifier !== null) {
            var isInitialRenoProcess = taskIdentifier[0].name.startsWith("IR : ");
            Xrm.WebApi.retrieveRecord("po_units", unit[0].id.replace('{', '').replace('}', ''), "?$select=po_unitid,po_unitidnum").then(
                function success(result) {
                    if (result.po_unitidnum !== null && result.po_unitidnum !== '' && result.po_unitidnum !== undefined) {
                        //Retrieve Task Identifier...
                        //var taskIdentifierName = taskIdentifier[0].name;
                        //alert(result.po_unitidnum);
                        switch (thisTaskName) {
                            case 'Vendor Says Job\'s Complete':
                                //Retrieve Job from Unit...
                                var queryOption = "$select=fkh_jobstatus,_fkh_vendor_value,fkh_yardicode&$filter=_fkh_unit_value eq " + unit[0].id.replace('{', '').replace('}', '') + " and  fkh_yardicode ne null";
                                //execute the query and get the results
                                Xrm.WebApi.retrieveMultipleRecords("fkh_jobs", queryOption)
                                    .then(function (data) {
                                        if (data.entities.length > 0) {
                                            var yardiJobID = data.entities[0]["fkh_yardicode"];
                                            var datapayLoad =
                                            {
                                                "fkh_eventdata": "[{'id': '" + Createguid() + "', 'eventType': 'allEvents', 'subject': " + (isInitialRenoProcess ? "'IR : VENDOR_SAYS_JOBS_COMPLETE'" : "'Turn Process : VENDOR_SAYS_JOBS_COMPLETE'") + ", 'eventTime': '" + dateTime + "', 'data': { 'PropertyID': '" + result.po_unitidnum + "', 'YardiJobCode' : '" + yardiJobID + "', 'Event': " + (isInitialRenoProcess ? "214" : "15") + ", 'Date1': '" + dateTime + "', 'IsForce': false}, 'Topic': '' }]",
                                                "fkh_direction": true,
                                                "fkh_name": isInitialRenoProcess ? "IR : VENDOR_SAYS_JOBS_COMPLETE" : "Turn Process : VENDOR_SAYS_JOBS_COMPLETE"
                                            };

                                            // create account record
                                            Xrm.WebApi.createRecord("fkh_azureintegrationcalls", datapayLoad).then(
                                                function success(result) {
                                                    console.log("Azure Integration Call created with ID: " + result.id);
                                                    // perform operations on record creation
                                                },
                                                function (error) {
                                                    console.log(error.message);
                                                    alert(error.message);
                                                    // handle error conditions
                                                }
                                            );
                                        }
                                        else {
                                            alert('No Jobs found for Property.');
                                        }
                                    },
                                        function (error) {
                                            Xrm.Utility.alertDialog(error.message);
                                        }
                                    );

                                break;
                            case 'Job Completed':
                                var datapayLoad =
                                {
                                    "fkh_eventdata": "[{'id': '" + Createguid() + "', 'eventType': 'allEvents', 'subject': " + (isInitialRenoProcess ? "'IR : JOB_COMPLETED'" : "'Turn Process : JOB_COMPLETED'") + ", 'eventTime': '" + dateTime + "', 'data': { 'PropertyID': '" + result.po_unitidnum + "', 'Event': " + (isInitialRenoProcess ? "216" : "17") + ", 'Date1': '" + dateTime + "', 'IsForce': false}, 'Topic': '' }]",
                                    "fkh_direction": true,
                                    "fkh_name": isInitialRenoProcess ? "IR : JOB_COMPLETED" : "Turn Process : JOB_COMPLETED"
                                };
                                // create account record
                                Xrm.WebApi.createRecord("fkh_azureintegrationcalls", datapayLoad).then(
                                    function success(result) {
                                        console.log("Azure Integration Call created with ID: " + result.id);
                                        // perform operations on record creation
                                    },
                                    function (error) {
                                        console.log(error.message);
                                        alert(error.message);
                                        // handle error conditions
                                    }
                                );
                                break;
                        }



                    }

                    //console.log(`Retrieved values: Name: ${result.name}, Revenue: ${result.revenue}`);
                    // perform operations on record retrieval
                },
                function (error) {
                    Xrm.Utility.alertDialog(error.message);
                    // handle error conditions
                }
            );
        }
    }
};

function Createguid() {
    function s4() {
        return Math.floor((1 + Math.random()) * 0x10000)
            .toString(16)
            .substring(1);
    }
    return s4() + s4() + '-' + s4() + '-' + s4() + '-' + s4() + '-' + s4() + s4() + s4();
}