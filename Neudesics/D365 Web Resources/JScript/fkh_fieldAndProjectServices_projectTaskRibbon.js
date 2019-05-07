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
            var contractID = Xrm.Page.getAttribute("fkh_contractid").getValue();
            var thisProjectTaskId = Xrm.Page.data.entity.getId().toString().replace("{", "").replace("}", "");
            var thisProjectId = Xrm.Page.getAttribute("msdyn_project").getValue()[0].id.toString().replace("{", "").replace("}", "");
            switch (thisTaskName) {
                case 'Pre-Move-Out Inspection':
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.completeThisTask();
                    break;
                case 'Move-Out Inspection':
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.completeThisTask();
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.startTask('Budget Start', thisProjectId, null);
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.completeTask('Corporate Renewals', thisProjectId, null);
                    break;
                // case 'Budget Start':
                //     FKH.FieldAndProjectServices.ProjectTaskRibbon.completeThisTask();
                //     FKH.FieldAndProjectServices.ProjectTaskRibbon.startTask('Budget Approval', thisProjectId, null);
                //     break;
                // case 'Budget Approval':
                //     FKH.FieldAndProjectServices.ProjectTaskRibbon.completeThisTask();
                //     FKH.FieldAndProjectServices.ProjectTaskRibbon.startTask('Job Assignment to Vendor(s) in Contract Creator', thisProjectId, null);
                //     FKH.FieldAndProjectServices.ProjectTaskRibbon.startTask('Job Assignment to Vendor(s)', thisProjectId, null);
                //     FKH.FieldAndProjectServices.ProjectTaskRibbon.startTask('Offer Rejected or Approved', thisProjectId, null);
                //     break;
                case 'Vendor(s) Says Job Started':
                case 'Vendor Says Job Started':
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.completeThisTask();
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.startTask('Work in Progress', thisProjectId, contractID);
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.publishMessage(thisTaskName);
                    break;
                case 'Work In Progress':
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.completeThisTask();
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.completeTask('Vendor Says Job\'s Complete', thisProjectId, contractID);
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.startTask('Quality Control Inspection', thisProjectId, contractID);
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.publishMessage('Vendor Says Job\'s Complete');
                    break;
                case 'Vendor Says Job\'s Complete':
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.completeThisTask();
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.completeTask('Work In Progress', thisProjectId, contractID);
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.startTask('Quality Control Inspection', thisProjectId, contractID);
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.publishMessage(thisTaskName);
                    break;
                case 'Hero Shot Picture':
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.completeThisTask();
                    isComplete = FKH.FieldAndProjectServices.ProjectTaskRibbon.isTaskComplete('Move-In Inspection', thisProjectId);
                    if (isComplete) {
                        FKH.FieldAndProjectServices.ProjectTaskRibbon.completeProject(thisProjectId);
                    }
                    break;
                case 'Marketing Inspection':
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.completeThisTask();
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.scheduleBiWeeklyInspection(thisProjectId);
                    break;
                case 'Bi-Weekly Inspection':
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.completeThisTask();
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.createBiWeeklyInspection(thisProjectTaskId, thisProjectId);
                    break;
                case 'Move-In Inspection':
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.completeThisTask();
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.completeTask('Bi-Weekly Inspection', thisProjectId, null);
                    isComplete = FKH.FieldAndProjectServices.ProjectTaskRibbon.isTaskComplete('Hero Shot Picture', thisProjectId);
                    if (isComplete) {
                        FKH.FieldAndProjectServices.ProjectTaskRibbon.completeProject(thisProjectId);
                    }
                    break;
                default:
            }
            Xrm.Page.data.refresh(true);
        }
    },

    onClick_StartTask: function () {
        var thisTaskName = Xrm.Page.getAttribute("msdyn_subject").getValue();
        FKH.FieldAndProjectServices.ProjectTaskRibbon.startThisTask();
        switch (thisTaskName) {
            case 'Vendor(s) Says Job Started':
            case 'Vendor Says Job Started':
                FKH.FieldAndProjectServices.ProjectTaskRibbon.publishMessage(thisTaskName);
                break;

        }
    },

    onClick_Pass: function () {
        var thisProjectId = Xrm.Page.getAttribute("msdyn_project").getValue()[0].id;
        var contractID = Xrm.Page.getAttribute("fkh_contractid").getValue();
        FKH.FieldAndProjectServices.ProjectTaskRibbon.completeThisTask();
        FKH.FieldAndProjectServices.ProjectTaskRibbon.completeTask('Job Completed', thisProjectId, contractID);
        FKH.FieldAndProjectServices.ProjectTaskRibbon.startTask('Marketing Inspection', thisProjectId, null);
        FKH.FieldAndProjectServices.ProjectTaskRibbon.publishMessage('Job Completed');
        Xrm.Page.data.refresh(true);
    },

    onClick_Fail: function () {
        var thisProjectTaskId = Xrm.Page.data.entity.getId().toString().replace("{", "").replace("}", "");
        var thisProjectId = Xrm.Page.getAttribute("msdyn_project").getValue()[0].id;
        FKH.FieldAndProjectServices.ProjectTaskRibbon.completeThisTask();
        FKH.FieldAndProjectServices.ProjectTaskRibbon.createReWorkTasks(thisProjectTaskId, thisProjectId);
        Xrm.Page.data.refresh(true);
    },

    completeThisTask: function () {
        var currentDatetime = new Date();
        if (Xrm.Page.getAttribute("msdyn_actualstart").getValue() == null) {
            Xrm.Page.getAttribute("msdyn_actualstart").setValue(new Date(currentDatetime.setHours(currentDatetime.getHours(), currentDatetime.getMinutes() - 1, 0)));
        }
        Xrm.Page.getAttribute("msdyn_actualend").setValue(new Date(currentDatetime.setHours(currentDatetime.getHours(), currentDatetime.getMinutes(), 0)));
        Xrm.Page.getAttribute("msdyn_progress").setValue(100);
        Xrm.Page.getAttribute("msdyn_progress").setSubmitMode("always");
        var startDate = Xrm.Page.getAttribute("msdyn_actualstart").getValue();
        var endDate = Xrm.Page.getAttribute("msdyn_actualend").getValue();
        var minutesBetween = FKH.FieldAndProjectServices.ProjectTaskRibbon.dateDiffInMinutes(startDate, endDate);
        Xrm.Page.getAttribute("msdyn_actualdurationminutes").setValue(minutesBetween);
        Xrm.Page.getAttribute("statuscode").setValue(963850001);//Completed
        FKH.FieldAndProjectServices.ProjectTaskRibbon.completeParentTask(Xrm.Page.getAttribute("msdyn_parenttask").getValue()[0].id.toString().replace("{", "").replace("}", ""), Xrm.Page.data.entity.getId().toString().replace("{", "").replace("}", ""));
    },

    startThisTask: function () {
        Xrm.Page.getAttribute("msdyn_actualstart").setValue(new Date());
        Xrm.Page.getAttribute("msdyn_progress").setValue(1);
        Xrm.Page.getAttribute("statuscode").setValue(963850000);//In Progress
        if (Xrm.Page.getAttribute("msdyn_parenttask") != null && Xrm.Page.getAttribute("msdyn_parenttask").getValue() != null) {
            FKH.FieldAndProjectServices.ProjectTaskRibbon.startParentTask(Xrm.Page.getAttribute("msdyn_parenttask").getValue()[0].id.toString().replace("{", "").replace("}", ""));
        }
    },

    isVisible_MarkComplete: function () {
        var thisProjectTaskId = Xrm.Page.data.entity.getId().toString().replace("{", "").replace("}", "");
        var taskName = Xrm.Page.getAttribute("msdyn_subject").getValue();
        var taskStatus = Xrm.Page.getAttribute("statuscode").getValue();
        if (Xrm.Page.getAttribute("msdyn_subject") != null && Xrm.Page.getAttribute("msdyn_subject").getValue() != null && taskStatus != 963850001 /*Completed*/) {
            if (FKH.FieldAndProjectServices.ProjectTaskRibbon.isParentTask()) {
                Xrm.Page.ui.clearFormNotification("warnParent");
                Xrm.Page.ui.setFormNotification("This is a parent-level project task and is started and completed automatically when the tasks underneath it have been started and completed.", "WARNING", "warnParent");
                return false;
            } else if (FKH.FieldAndProjectServices.ProjectTaskRibbon.predecessorsAreComplete(thisProjectTaskId)) {
                switch (taskName) {
                    case 'Pre-Move-Out Inspection':
                    case 'Move-Out Inspection':
                    // case 'Budget Start':
                    // case 'Budget Approval':
                    case 'Vendor(s) Says Job Started':
                    case 'Vendor Says Job Started':
                    case 'Work In Progress':
                    case 'Vendor Says Job\'s Complete':
                    case 'Hero Shot Picture':
                    case 'Marketing Inspection':
                    case 'Bi-Weekly Inspection':
                    case 'Move-In Inspection':
                        return true;
                    case 'Quality Control Inspection':
                        return false;
                    default:
                        Xrm.Page.ui.clearFormNotification("warnAuto");
                        Xrm.Page.ui.setFormNotification("This project task is completed automatically and cannot be completed manually.", "WARNING", "warnAuto");
                        return false;
                }
            } else {
                switch (taskName) {
                    case 'Pre-Move-Out Inspection':
                    case 'Move-Out Inspection':
                    // case 'Budget Start':
                    // case 'Budget Approval':
                    case 'Vendor(s) Says Job Started':
                    case 'Vendor Says Job Started':
                    case 'Work In Progress':
                    case 'Vendor Says Job\'s Complete':
                    case 'Hero Shot Picture':
                    case 'Marketing Inspection':
                    case 'Bi-Weekly Inspection':
                    case 'Move-In Inspection':
                        Xrm.Page.ui.clearFormNotification("warnPred");
                        Xrm.Page.ui.setFormNotification("This project task cannot be completed until its predecessor has been completed.", "WARNING", "warnPred");
                        break;
                    case 'Quality Control Inspection':
                        break;
                    default:
                        Xrm.Page.ui.clearFormNotification("wardAuto");
                        Xrm.Page.ui.setFormNotification("This project task is completed automatically and cannot be completed manually.", "WARNING", "wardAuto");
                }
            }
        }
        return false;
    },

    isVisible_Hidden: function () {
        return false;
    },

    isVisible_StartTask: function () {
        var taskStatus = Xrm.Page.getAttribute("statuscode").getValue();
        if (taskStatus == 1/*Not Started*/) {
            if (FKH.FieldAndProjectServices.ProjectTaskRibbon.isParentTask()) {
                Xrm.Page.ui.clearFormNotification("warnParent");
                Xrm.Page.ui.setFormNotification("This is a parent-level project task and is started and completed automatically when the tasks underneath it have been started and completed.", "WARNING", "warnParent");
                return false;
            } else {
                return true;
            }
        }
        return false;
    },

    isVisible_PassFail: function () {
        if (Xrm.Page.getAttribute("msdyn_subject") != null && Xrm.Page.getAttribute("msdyn_subject").getValue() != null) {
            var taskName = Xrm.Page.getAttribute("msdyn_subject").getValue();
            var taskStatus = Xrm.Page.getAttribute("statuscode").getValue();
            if (taskStatus != 963850001) {//Completed
                var thisProjectTaskId = Xrm.Page.data.entity.getId().toString().replace("{", "").replace("}", "");
                if (FKH.FieldAndProjectServices.ProjectTaskRibbon.isParentTask()) {
                    Xrm.Page.ui.clearFormNotification("warnParent");
                    Xrm.Page.ui.setFormNotification("This is a parent-level project task and is started and completed automatically when the tasks underneath it have been started and completed.", "WARNING", "warnParent");
                    return false;
                } else if (FKH.FieldAndProjectServices.ProjectTaskRibbon.predecessorsAreComplete(thisProjectTaskId)) {
                    switch (taskName) {
                        case 'Quality Control Inspection':
                            return true;
                        default:
                            return false;
                    }
                } else {
                    switch (taskName) {
                        case 'Quality Control Inspection':
                            Xrm.Page.ui.clearFormNotification("warnPred");
                            Xrm.Page.ui.setFormNotification("This project task cannot be completed until its predecessor has been completed.", "WARNING", "warnPred");
                            return false;
                        default:
                            return false;
                    }
                }
            }
        }
        return false;
    },

    isParentTask: function () {
        if (Xrm.Page.getAttribute("msdyn_resourceutilization") != null && Xrm.Page.getAttribute("msdyn_resourceutilization").getValue() != null && Xrm.Page.getAttribute("msdyn_resourceutilization").getValue() != "") {
            return false;
        } else {
            return true;
        }
    },

    startParentTask: function (parentTaskId) {
        var getReq = new XMLHttpRequest();
        getReq.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/msdyn_projecttasks?$select=msdyn_actualstart,msdyn_progress,statuscode&$filter=msdyn_projecttaskid eq " + parentTaskId, true);
        getReq.setRequestHeader("OData-MaxVersion", "4.0");
        getReq.setRequestHeader("OData-Version", "4.0");
        getReq.setRequestHeader("Accept", "application/json");
        getReq.setRequestHeader("Content-Type", "application/json; charset=utf-8");
        getReq.onreadystatechange = function () {
            if (this.readyState === 4) {
                getReq.onreadystatechange = null;
                if (this.status === 200) {
                    var results = JSON.parse(this.response);
                    if (results.value.length != 0) {
                        var entity = {};
                        var submitUpdate = false;
                        if (results.value[0]["msdyn_actualstart"] == null) {
                            entity.msdyn_actualstart = new Date().toISOString();
                            submitUpdate = true;
                        }
                        if (results.value[0]["msdyn_progress"] == null || results.value[0]["msdyn_progress"] == 0) {
                            entity.msdyn_progress = 1;
                            submitUpdate = true;
                        }
                        if (results.value[0]["statuscode"] != 963850000) {
                            entity.statuscode = 963850000; //In Progress
                            submitUpdate = true;
                        }

                        if (submitUpdate) {
                            var updateReq = new XMLHttpRequest();
                            updateReq.open("PATCH", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/msdyn_projecttasks(" + parentTaskId + ")", true);
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
                                        Xrm.Utility.alertDialog("Error in FKH.FieldAndProjectServices.ProjectTaskRibbon.startParentTask2: " + this.response);
                                    }
                                }
                            };
                            updateReq.send(JSON.stringify(entity));
                        }
                    }
                } else {
                    Xrm.Utility.alertDialog("Error in FKH.FieldAndProjectServices.ProjectTaskRibbon.startParentTask1: " + this.response);
                }
            }
        };
        getReq.send();
    },

    completeParentTask: function (parentTaskId, thisChildTaskId) {
        var getReq = new XMLHttpRequest();
        getReq.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/msdyn_projecttasks?$select=msdyn_actualstart,msdyn_progress,statuscode,msdyn_projecttaskid&$filter=_msdyn_parenttask_value eq " + parentTaskId + " and  msdyn_projecttaskid ne " + thisChildTaskId + " and statuscode ne 963850001 and statuscode ne 2", true);
        getReq.setRequestHeader("OData-MaxVersion", "4.0");
        getReq.setRequestHeader("OData-Version", "4.0");
        getReq.setRequestHeader("Accept", "application/json");
        getReq.setRequestHeader("Content-Type", "application/json; charset=utf-8");
        getReq.onreadystatechange = function () {
            if (this.readyState === 4) {
                getReq.onreadystatechange = null;
                if (this.status === 200) {
                    var results = JSON.parse(this.response);
                    if (results.value.length == 0) {
                        var getTaskReq = new XMLHttpRequest();
                        getTaskReq.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/msdyn_projecttasks?$select=msdyn_actualstart&$filter=msdyn_projecttaskid eq " + parentTaskId, true);
                        getTaskReq.setRequestHeader("OData-MaxVersion", "4.0");
                        getTaskReq.setRequestHeader("OData-Version", "4.0");
                        getTaskReq.setRequestHeader("Accept", "application/json");
                        getTaskReq.setRequestHeader("Content-Type", "application/json; charset=utf-8");
                        getTaskReq.onreadystatechange = function () {
                            if (this.readyState === 4) {
                                getTaskReq.onreadystatechange = null;
                                if (this.status === 200) {
                                    var innerResults = JSON.parse(this.response);
                                    if (innerResults.value.length != 0) {
                                        var entity = {};
                                        var startDate;
                                        var currentDatetime = new Date().toISOString();
                                        if (innerResults.value[0]["msdyn_actualstart"] == null) {
                                            entity.msdyn_actualstart = currentDatetime;
                                            startDate = new Date();
                                        } else {
                                            startDate = new Date(innerResults.value[0]["msdyn_actualstart"]);
                                        }
                                        entity.msdyn_actualend = currentDatetime;
                                        endDate = new Date();
                                        var minutesBetween = FKH.FieldAndProjectServices.ProjectTaskRibbon.dateDiffInMinutes(startDate, endDate);
                                        try {
                                            entity.msdyn_actualdurationminutes = minutesBetween;
                                        } catch (err) {
                                            Xrm.Utility.alertDialog(err);
                                        }
                                        entity.msdyn_progress = 100;
                                        entity.statuscode = 963850001; //Completed

                                        var updateReq = new XMLHttpRequest();
                                        updateReq.open("PATCH", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/msdyn_projecttasks(" + parentTaskId + ")", true);
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
                                                    Xrm.Utility.alertDialog("Error in FKH.FieldAndProjectServices.ProjectTaskRibbon.completeParentTask2: " + this.response);
                                                }
                                            }
                                        };
                                        updateReq.send(JSON.stringify(entity));
                                    }
                                } else {
                                    Xrm.Utility.alertDialog("Error in FKH.FieldAndProjectServices.ProjectTaskRibbon.startParentTask1: " + this.response);
                                }
                            }
                        };
                        getTaskReq.send();
                    }
                } else {
                    Xrm.Utility.alertDialog("Error in FKH.FieldAndProjectServices.ProjectTaskRibbon.completeParentTask1: " + this.response);
                }
            }
        };
        getReq.send();
    },

    startTask: function (taskName, projectId, contractID) {
        var getReq = new XMLHttpRequest();
        var filter = "_msdyn_project_value eq " + projectId + " and contains(msdyn_subject, '" + taskName.replace("'", "%") + "') and statuscode ne 963850001 and statuscode ne 2";
        if (contractID != null) {
            filter += " and fkh_contractid eq '" + contractID + "'";
        }
        getReq.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/msdyn_projecttasks?$select=_msdyn_parenttask_value,msdyn_actualstart,msdyn_progress,statuscode,msdyn_projecttaskid&$filter=" + filter, true);
        getReq.setRequestHeader("OData-MaxVersion", "4.0");
        getReq.setRequestHeader("OData-Version", "4.0");
        getReq.setRequestHeader("Accept", "application/json");
        getReq.setRequestHeader("Content-Type", "application/json; charset=utf-8");
        getReq.setRequestHeader("Prefer", "odata.include-annotations=\"*\"");
        getReq.onreadystatechange = function () {
            if (this.readyState === 4) {
                getReq.onreadystatechange = null;
                if (this.status === 200) {
                    var results = JSON.parse(this.response);
                    for (var i = 0; i < results.value.length; i++) {
                        var entity = {};
                        var submitUpdate = false;
                        if (results.value[i]["msdyn_actualstart"] == null) {
                            entity.msdyn_actualstart = new Date().toISOString();
                            submitUpdate = true;
                        }
                        if (results.value[i]["msdyn_progress"] == null || results.value[i]["msdyn_progress"] == 0) {
                            entity.msdyn_progress = 1;
                            submitUpdate = true;
                        }
                        if (results.value[i]["statuscode"] != 963850000) {
                            entity.statuscode = 963850000; //In Progress
                            submitUpdate = true;
                        }

                        if (submitUpdate) {
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
                                        Xrm.Utility.alertDialog("The '" + taskName + "' task may not have been started properly.  Please inform the CRM Help Desk. (" + this.response + ")");
                                    }
                                }
                            };
                            updateReq.send(JSON.stringify(entity));
                            if (results.value[i]["_msdyn_parenttask_value"] != null && results.value[i]["_msdyn_parenttask_value"] != "") {
                                FKH.FieldAndProjectServices.ProjectTaskRibbon.startParentTask(results.value[i]["_msdyn_parenttask_value"]);
                            }
                        }
                    }
                } else {
                    Xrm.Utility.alertDialog("The '" + taskName + "' task may not have been started properly.  Please inform the CRM Help Desk. (" + this.response + ")");
                }
            }
        };
        getReq.send();
    },

    completeTask: function (taskName, projectId, contractID) {
        var getReq = new XMLHttpRequest();
        var filter = "_msdyn_project_value eq " + projectId + " and contains(msdyn_subject, '" + taskName.replace("'", "%") + "') and statuscode ne 963850001 and statuscode ne 2";
        if (contractID != null) {
            filter += " and fkh_contractid eq '" + contractID + "'";
        }
        getReq.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/msdyn_projecttasks?$select=_msdyn_parenttask_value,msdyn_actualstart&$filter=" + filter, true);
        getReq.setRequestHeader("OData-MaxVersion", "4.0");
        getReq.setRequestHeader("OData-Version", "4.0");
        getReq.setRequestHeader("Accept", "application/json");
        getReq.setRequestHeader("Content-Type", "application/json; charset=utf-8");
        getReq.onreadystatechange = function () {
            if (this.readyState === 4) {
                getReq.onreadystatechange = null;
                if (this.status === 200) {
                    var results = JSON.parse(this.response);
                    for (var i = 0; i < results.value.length; i++) {
                        var entity = {};
                        var startDate;
                        var currentDatetime = new Date().toISOString();
                        if (results.value[i]["msdyn_actualstart"] == null) {
                            entity.msdyn_actualstart = currentDatetime;
                            startDate = new Date();
                        } else {
                            startDate = new Date(results.value[i]["msdyn_actualstart"]);
                        }
                        entity.msdyn_actualend = currentDatetime;
                        endDate = new Date();
                        var minutesBetween = FKH.FieldAndProjectServices.ProjectTaskRibbon.dateDiffInMinutes(startDate, endDate);
                        try {
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
                                    Xrm.Utility.alertDialog("The '" + taskName + "' task may not have updated properly.  Please inform the CRM Help Desk. (" + this.response + ")");
                                }
                            }
                        };
                        updateReq.send(JSON.stringify(entity));
                        FKH.FieldAndProjectServices.ProjectTaskRibbon.completeParentTask(results.value[i]["_msdyn_parenttask_value"], results.value[i]["msdyn_projecttaskid"])
                    }
                } else {
                    Xrm.Utility.alertDialog("Error in FKH.FieldAndProjectServices.ProjectTaskRibbon.completeTask: " + this.response);
                }
            }
        };
        getReq.send();
    },

    completeProject: function (projectId) {
        var entity = {};
        entity.msdyn_actualend = new Date().toISOString();
        entity.statecode = 1;
        entity.statuscode = 192350000;

        var req = new XMLHttpRequest();
        req.open("PATCH", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/msdyn_projects(" + projectId + ")", true);
        req.setRequestHeader("OData-MaxVersion", "4.0");
        req.setRequestHeader("OData-Version", "4.0");
        req.setRequestHeader("Accept", "application/json");
        req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
        req.onreadystatechange = function () {
            if (this.readyState === 4) {
                req.onreadystatechange = null;
                if (this.status === 204) {
                    //Success - No Return Data - Do Something
                } else {
                    Xrm.Utility.alertDialog("Error in FKH.FieldAndProjectServices.ProjectTaskRibbon.completeProject:  " + this.response);
                }
            }
        };
        req.send(JSON.stringify(entity));
    },

    isTaskComplete: function (taskName, projectId) {
        var isComplete;
        var req = new XMLHttpRequest();
        req.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/msdyn_projecttasks?$filter=_msdyn_project_value eq " + projectId + " and  msdyn_subject eq '" + taskName + "' and statuscode ne 963850001 and statuscode ne 2", false);
        req.setRequestHeader("OData-MaxVersion", "4.0");
        req.setRequestHeader("OData-Version", "4.0");
        req.setRequestHeader("Accept", "application/json");
        req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
        req.setRequestHeader("Prefer", "odata.include-annotations=\"*\"");
        req.onreadystatechange = function () {
            if (this.readyState === 4) {
                req.onreadystatechange = null;
                if (this.status === 200) {
                    var results = JSON.parse(this.response);
                    if (results.value.length > 0) {
                        isComplete = false;
                    } else {
                        isComplete = true;
                    }
                } else {
                    Xrm.Utility.alertDialog("Error in FKH.FieldAndProjectServices.ProjectTaskRibbon.isTaskComplete: " + this.response);
                }
            }
        };
        req.send();
        return isComplete;
    },

    publishMessage: function (thisTaskName) {
        //alert("Publish Message");
        var unit = Xrm.Page.getAttribute('fkh_unitid').getValue();
        var taskIdentifier = Xrm.Page.getAttribute('fkh_taskidentifierid').getValue();
        var today = new Date();
        var date = today.getFullYear() + '-' + (today.getMonth() + 1) + '-' + today.getDate();
        var time = today.getHours() + ":" + today.getMinutes() + ":" + today.getSeconds();
        var dateTime = date + ' ' + time;

        if (unit !== null && taskIdentifier !== null) {
            var isInitialRenoProcess = taskIdentifier[0].name.startsWith("IR : ");
            Xrm.WebApi.retrieveRecord("po_units", unit[0].id.replace('{', '').replace('}', ''), "?$select=po_unitid,po_unitidnum,fkh_sfcode").then(
                function success(result) {
                    if ((result.po_unitidnum !== null && result.po_unitidnum !== '' && result.po_unitidnum !== undefined) || (result.fkh_sfcode !== null && result.fkh_sfcode !== '' && result.fkh_sfcode !== undefined)) {
                        //Retrieve Task Identifier...
                        //var taskIdentifierName = taskIdentifier[0].name;
                        //alert(result.po_unitidnum);
                        //Retrieve Job from Unit...

                        if (thisTaskName === "Vendor(s) Says Job Started" || thisTaskName === "Vendor Says Job Started") {
                            Process.callAction("fkh_VendorSaysJobStarted",
                                [
                                    {
                                        key: "Target",
                                        type: Process.Type.EntityReference,
                                        value: { id: Xrm.Page.data.entity.getId().replace('{', '').replace('}', ''), entityType: 'msdyn_projecttask' }
                                    }
                                ],
                                function (params) {
                                    //debugger;
                                    // Success                      
                                    if (params.length > 0) {
                                        if (params[0].key === "IsSuccess" && params[0].value === "true") {
                                            Xrm.Page.data.refresh(false).then(function successCallback() {
                                                Xrm.Page.ui.refreshRibbon(); Xrm.Page.data.refresh();
                                            });
                                        }
                                        else {
                                            if (params[1].key === "ErrorMessage" && params[1].value !== null && params[1].value !== "" && params[1].value !== undefined)
                                                alert("Error : " + params[1].value);
                                        }
                                        Xrm.Page.data.refresh(true);
                                    }
                                    else {
                                        Xrm.Page.data.refresh(true);
                                        Xrm.Page.ui.refreshRibbon();
                                    }
                                },
                                function (e) {
                                    // Error
                                    alert("Error!", e, null, "ERROR", 500, 200);
                                    Xrm.Page.data.refresh(true);
                                    Xrm.Page.ui.refreshRibbon();
                                }
                            );
                        }
                        else if (thisTaskName === "Job Completed") {
                            Process.callAction("fkh_ContractCompleted",
                                [
                                    {
                                        key: "Target",
                                        type: Process.Type.EntityReference,
                                        value: { id: Xrm.Page.data.entity.getId().replace('{', '').replace('}', ''), entityType: 'msdyn_projecttask' }
                                    }
                                ],
                                function (params) {
                                    //debugger;
                                    // Success                      
                                    if (params.length > 0) {
                                        if (params[0].key === "IsSuccess" && params[0].value === "true") {
                                            Xrm.Page.data.refresh(false).then(function successCallback() {
                                                Xrm.Page.ui.refreshRibbon(); Xrm.Page.data.refresh();
                                            });
                                        }
                                        else {
                                            if (params[1].key === "ErrorMessage" && params[1].value !== null && params[1].value !== "" && params[1].value !== undefined)
                                                alert("Error : " + params[1].value);
                                        }
                                        Xrm.Page.data.refresh(true);
                                    }
                                    else {
                                        Xrm.Page.data.refresh(true);
                                        Xrm.Page.ui.refreshRibbon();
                                    }
                                },
                                function (e) {
                                    // Error
                                    alert("Error!", e, null, "ERROR", 500, 200);
                                    Xrm.Page.data.refresh(true);
                                    Xrm.Page.ui.refreshRibbon();
                                }
                            );
                        }
                        else {
                            var queryOption = "$select=fkh_jobstatus,_fkh_vendor_value,fkh_renowalkid&$filter=_fkh_unit_value eq " + unit[0].id.replace('{', '').replace('}', '') + " and  fkh_renowalkid ne null";
                            var unitcode = null;
                            if (result.po_unitidnum !== null && result.po_unitidnum !== '' && result.po_unitidnum !== undefined)
                                unitcode = result.po_unitidnum;
                            else if (result.fkh_sfcode !== null && result.fkh_sfcode !== '' && result.fkh_sfcode !== undefined)
                                unitcode = result.fkh_sfcode;
                            //execute the query and get the results
                            Xrm.WebApi.retrieveMultipleRecords("fkh_jobs", queryOption)
                                .then(function (data) {
                                    if (data.entities.length > 0) {
                                        var renowalkID = data.entities[0]["fkh_renowalkid"];
                                        var datapayLoad = '', incomingDataPayLoad = '';
                                        switch (thisTaskName) {
                                            case 'Vendor Says Job\'s Complete':
                                                datapayLoad =
                                                    {
                                                        "fkh_eventdata": "[{'id': '" + Createguid() + "', 'eventType': 'allEvents', 'subject': " + (isInitialRenoProcess ? "'IR : VENDOR_SAYS_JOBS_COMPLETE'" : "'Turn Process : VENDOR_SAYS_JOBS_COMPLETE'") + ", 'eventTime': '" + dateTime + "', 'data': { 'PropertyID': '" + unitcode + "', 'JobID' : '" + renowalkID + "', 'Event': " + (isInitialRenoProcess ? "214" : "15") + ", 'Date1': '" + dateTime + "', 'IsForce': false}, 'Topic': '' }]",
                                                        "fkh_direction": true,
                                                        "fkh_name": isInitialRenoProcess ? "IR : VENDOR_SAYS_JOBS_COMPLETE" : "Turn Process : VENDOR_SAYS_JOBS_COMPLETE"
                                                    };
                                                incomingDataPayLoad =
                                                    {
                                                        "fkh_eventdata": "[{'id': '" + Createguid() + "', 'eventType': 'allEvents', 'subject': " + (isInitialRenoProcess ? "'IR : VENDOR_SAYS_JOBS_COMPLETE'" : "'Turn Process : VENDOR_SAYS_JOBS_COMPLETE'") + ", 'eventTime': '" + dateTime + "', 'data': { 'PropertyID': '" + unitcode + "', 'JobID' : '" + renowalkID + "', 'Event': " + (isInitialRenoProcess ? "214" : "15") + ", 'Date1': '" + dateTime + "', 'IsForce': false}, 'Topic': '' }]",
                                                        "fkh_direction": false,
                                                        "fkh_name": isInitialRenoProcess ? "IR : VENDOR_SAYS_JOBS_COMPLETE" : "Turn Process : VENDOR_SAYS_JOBS_COMPLETE"
                                                    };
                                                break;
                                            case 'Job Completed':
                                                datapayLoad =
                                                    {
                                                        "fkh_eventdata": "[{'id': '" + Createguid() + "', 'eventType': 'allEvents', 'subject': " + (isInitialRenoProcess ? "'IR : JOB_COMPLETED'" : "'Turn Process : JOB_COMPLETED'") + ", 'eventTime': '" + dateTime + "', 'data': { 'PropertyID': '" + unitcode + "', 'JobID' : '" + renowalkID + "', 'Event': " + (isInitialRenoProcess ? "216" : "17") + ", 'Date1': '" + dateTime + "', 'IsForce': false}, 'Topic': '' }]",
                                                        "fkh_direction": true,
                                                        "fkh_name": isInitialRenoProcess ? "IR : JOB_COMPLETED" : "Turn Process : JOB_COMPLETED"
                                                    };
                                                break;

                                        }
                                        if (datapayLoad !== '') {
                                            Xrm.WebApi.createRecord("fkh_azureintegrationcalls", datapayLoad).then(
                                                function success(result) {
                                                    console.log("Azure Integration Call created with ID: " + result.id);
                                                    // perform operations on record creation
                                                },
                                                function (error) {
                                                    console.log("Error in FKH.FieldAndProjectServices.ProjectTaskRibbon.publishMessage:  " + error.message);
                                                    alert("Error in FKH.FieldAndProjectServices.ProjectTaskRibbon.publishMessage:  " + error.message);
                                                    // handle error conditions
                                                }
                                            );
                                        }
                                        if (incomingDataPayLoad !== '') {
                                            Xrm.WebApi.createRecord("fkh_azureintegrationcalls", incomingDataPayLoad).then(
                                                function success(result) {
                                                    console.log("Azure Integration Call created with ID: " + result.id);
                                                    // perform operations on record creation
                                                },
                                                function (error) {
                                                    console.log("Error in FKH.FieldAndProjectServices.ProjectTaskRibbon.publishMessage:  " + error.message);
                                                    alert("Error in FKH.FieldAndProjectServices.ProjectTaskRibbon.publishMessage:  " + error.message);
                                                    // handle error conditions
                                                }
                                            );
                                        }


                                    }
                                    else {
                                        alert('No Jobs found for Property.');
                                    }
                                },
                                    function (error) {
                                        Xrm.Utility.alertDialog("Error in FKH.FieldAndProjectServices.ProjectTaskRibbon.publishMessage:  " + error.message);
                                    }
                                );
                        }
                    }

                    //console.log(`Retrieved values: Name: ${result.name}, Revenue: ${result.revenue}`);
                    // perform operations on record retrieval
                },
                function (error) {
                    Xrm.Utility.alertDialog("Error in FKH.FieldAndProjectServices.ProjectTaskRibbon.publishMessage:  " + error.message);
                    // handle error conditions
                }
            );
        }
    },

    createReWorkTasks: function (thisProjectTaskId, thisProjectId) {
        var getReq = new XMLHttpRequest();
        getReq.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/msdyn_projecttasks(" + thisProjectTaskId + ")?$select=_msdyn_parenttask_value,fkh_contractid,_msdyn_assignedteammembers_value,fkh_accessnotes,fkh_lockboxremoved,fkh_mechanicallockbox,fkh_mechanicallockboxnote,fkh_propertygatecode,fkh_rentlylockbox,fkh_rentlylockboxnote,_fkh_taskidentifierid_value,_fkh_unitid_value,msdyn_costestimatecontour,msdyn_effort,msdyn_effortcontour,msdyn_effortestimateatcomplete,msdyn_progress,_msdyn_project_value,msdyn_remaininghours,_msdyn_resourceorganizationalunitid_value,msdyn_resourceutilization,msdyn_salesestimatecontour,msdyn_scheduledend,msdyn_scheduleddurationminutes,msdyn_scheduledhours,msdyn_scheduledstart,msdyn_subject,msdyn_wbsid,_ownerid_value,statuscode", true);
        getReq.setRequestHeader("OData-MaxVersion", "4.0");
        getReq.setRequestHeader("OData-Version", "4.0");
        getReq.setRequestHeader("Accept", "application/json");
        getReq.setRequestHeader("Content-Type", "application/json; charset=utf-8");
        getReq.onreadystatechange = function () {
            if (this.readyState === 4) {
                getReq.onreadystatechange = null;
                if (this.status === 200) {
                    var result = JSON.parse(this.response);
                    FKH.FieldAndProjectServices.ProjectTaskRibbon.createNextReWorkTask(result, thisProjectTaskId, thisProjectId, 'Work In Progress');
                } else {
                    Xrm.Utility.alertDialog("Error in FKH.FieldAndProjectServices.ProjectTaskRibbon.createReWorkTasks:  " + this.response);
                }
            }
        };
        getReq.send();
    },

    createNextReWorkTask: function (parentTask, predecessorProjectTaskId, thisProjectId, taskNameToCreate) {
        var entity = {};
        var dueDate = new Date();
        var newWbsId = "";
        var nexttaskNameToCreate = "";

        var req = new XMLHttpRequest();
        req.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/msdyn_projecttasks?$select=_fkh_taskidentifierid_value,_msdyn_parenttask_value,msdyn_wbsid&$filter=_msdyn_project_value eq " + thisProjectId.replace("{", "").replace("}", "") + " and contains(msdyn_subject, '" + taskNameToCreate.replace("'", "%") + "')", true);
        req.setRequestHeader("OData-MaxVersion", "4.0");
        req.setRequestHeader("OData-Version", "4.0");
        req.setRequestHeader("Accept", "application/json");
        req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
        req.setRequestHeader("Prefer", "odata.include-annotations=\"*\"");
        req.onreadystatechange = function () {
            if (this.readyState === 4) {
                req.onreadystatechange = null;
                if (this.status === 200) {
                    var results = JSON.parse(this.response);
                    var numberOfChildrenPlusParent = results.value.length;
                    if (numberOfChildrenPlusParent != null && numberOfChildrenPlusParent > 0) {
                        var _fkh_taskidentifierid_value = results.value[0]["_fkh_taskidentifierid_value"];
                        var _msdyn_parenttask_value = "";
                        for (var i = 0; i < results.value.length; i++) {
                            if (results.value[i]["_msdyn_parenttask_value"] != null && results.value[i]["_msdyn_parenttask_value"] != "") {
                                _msdyn_parenttask_value = results.value[i]["_msdyn_parenttask_value"];
                            } else if (results.value[i]["msdyn_wbsid"] != null & String(results.value[i]["msdyn_wbsid"]).indexOf(".") == -1) {
                                newWbsId = results.value[i]["msdyn_wbsid"] + "." + String(numberOfChildrenPlusParent);
                            }
                        }
                        switch (taskNameToCreate) {
                            case 'Work In Progress':
                                dueDate = dueDate.addDays(1);
                                nexttaskNameToCreate = "Vendor Says Job\'s Complete";
                                break;
                            case 'Vendor Says Job\'s Complete':
                                dueDate = dueDate.addDays(2);
                                nexttaskNameToCreate = "Quality Control Inspection";
                                break;
                            case 'Quality Control Inspection':
                                dueDate = dueDate.addDays(3);
                                break;
                            default:
                        }

                        entity.fkh_accessnotes = parentTask["fkh_accessnotes"];
                        entity.fkh_contractid = parentTask["fkh_contractid"];
                        entity.fkh_lockboxremoved = parentTask["fkh_lockboxremoved"];
                        entity.fkh_mechanicallockbox = parentTask["fkh_mechanicallockbox"];
                        entity.fkh_mechanicallockboxnote = parentTask["fkh_mechanicallockboxnote"];
                        entity.fkh_propertygatecode = parentTask["fkh_propertygatecode"];
                        entity.fkh_rentlylockbox = parentTask["fkh_rentlylockbox"];
                        entity.fkh_rentlylockboxnote = parentTask["fkh_rentlylockboxnote"];
                        entity.msdyn_costestimatecontour = parentTask["msdyn_costestimatecontour"];
                        entity.msdyn_effort = parentTask["msdyn_effort"];
                        entity.msdyn_effortcontour = parentTask["msdyn_effortcontour"];
                        entity.msdyn_effortestimateatcomplete = parentTask["msdyn_effortestimateatcomplete"];
                        entity.msdyn_progress = 0;
                        entity.msdyn_remaininghours = parentTask["msdyn_remaininghours"];
                        entity.msdyn_resourceutilization = parentTask["msdyn_resourceutilization"];
                        entity.msdyn_salesestimatecontour = parentTask["msdyn_salesestimatecontour"];
                        entity.msdyn_scheduledend = dueDate.addDays(1);
                        entity.msdyn_scheduleddurationminutes = 1440;//parentTask["msdyn_scheduleddurationminutes"];
                        entity.msdyn_scheduledhours = parentTask["msdyn_scheduledhours"];
                        entity.msdyn_scheduledstart = dueDate;
                        entity.msdyn_subject = taskNameToCreate;
                        entity.msdyn_wbsid = newWbsId;
                        entity.statuscode = 1;//Not Started
                        entity["fkh_TaskIdentifierId@odata.bind"] = "/fkh_taskidentifiers(" + _fkh_taskidentifierid_value + ")";
                        entity["fkh_UnitId@odata.bind"] = "/po_units(" + parentTask["_fkh_unitid_value"] + ")";
                        if (parentTask["_msdyn_assignedteammembers_value"] != null) {
                            entity["msdyn_AssignedTeamMembers@odata.bind"] = "/msdyn_projectteams(" + parentTask["_msdyn_assignedteammembers_value"] + ")";
                        }
                        if (_msdyn_parenttask_value != "") {
                            entity["msdyn_parenttask@odata.bind"] = "/msdyn_projecttasks(" + _msdyn_parenttask_value + ")";
                        }
                        entity["msdyn_project@odata.bind"] = "/msdyn_projects(" + parentTask["_msdyn_project_value"] + ")";
                        if (parentTask["_msdyn_resourceorganizationalunitid_value"] != null) {
                            entity["msdyn_ResourceOrganizationalUnitId@odata.bind"] = "/msdyn_organizationalunits(" + parentTask["_msdyn_resourceorganizationalunitid_value"] + ")";
                        }
                        entity["ownerid@odata.bind"] = "/systemusers(" + parentTask["_ownerid_value"] + ")";

                        var updateReq = new XMLHttpRequest();
                        updateReq.open("POST", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/msdyn_projecttasks", true);
                        updateReq.setRequestHeader("OData-MaxVersion", "4.0");
                        updateReq.setRequestHeader("OData-Version", "4.0");
                        updateReq.setRequestHeader("Accept", "application/json");
                        updateReq.setRequestHeader("Content-Type", "application/json; charset=utf-8");
                        updateReq.onreadystatechange = function () {
                            if (this.readyState === 4) {
                                updateReq.onreadystatechange = null;
                                if (this.status === 204) {
                                    var uri = this.getResponseHeader("OData-EntityId");
                                    var regExp = /\(([^)]+)\)/;
                                    var matches = regExp.exec(uri);
                                    var newEntityId = matches[1];
                                    FKH.FieldAndProjectServices.ProjectTaskRibbon.createProjectTaskDependency(predecessorProjectTaskId, newEntityId, thisProjectId);
                                    if (nexttaskNameToCreate != "") {
                                        FKH.FieldAndProjectServices.ProjectTaskRibbon.createNextReWorkTask(parentTask, newEntityId, thisProjectId, nexttaskNameToCreate);
                                    }
                                } else {
                                    Xrm.Utility.alertDialog("Error in FKH.FieldAndProjectServices.ProjectTaskRibbon.createNextReWorkTask(create project task):  " + this.response);
                                }
                            }
                        };
                        updateReq.send(JSON.stringify(entity));
                    }
                } else {
                    Xrm.Utility.alertDialog("Error in FKH.FieldAndProjectServices.ProjectTaskRibbon.createNextReWorkTask(get project tasks):  " + this.response);
                }
            }
        };
        req.send();
    },

    scheduleBiWeeklyInspection: function (thisProjectId) {
        var getReq = new XMLHttpRequest();
        getReq.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/msdyn_projecttasks?$select=msdyn_actualstart&$filter=_msdyn_project_value eq " + thisProjectId + " and  msdyn_subject eq 'Bi-Weekly Inspection' and statuscode ne 963850001 and statuscode ne 2", true);
        getReq.setRequestHeader("OData-MaxVersion", "4.0");
        getReq.setRequestHeader("OData-Version", "4.0");
        getReq.setRequestHeader("Accept", "application/json");
        getReq.setRequestHeader("Content-Type", "application/json; charset=utf-8");
        getReq.onreadystatechange = function () {
            if (this.readyState === 4) {
                getReq.onreadystatechange = null;
                if (this.status === 200) {
                    var results = JSON.parse(this.response);
                    var startDate = new Date();
                    for (var i = 0; i < results.value.length; i++) {
                        var entity = {};
                        entity.msdyn_scheduledstart = startDate.addDays(13);
                        entity.msdyn_scheduledend = startDate.addDays(14);
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
                                    Xrm.Utility.alertDialog("The 'Bi-Weekly Inspection' task may not have been started properly.  Please inform the CRM Help Desk. (" + this.response + ")");
                                }
                            }
                        };
                        updateReq.send(JSON.stringify(entity));
                    }
                } else {
                    Xrm.Utility.alertDialog("The '" + taskName + "' task may not have been started properly.  Please inform the CRM Help Desk. (" + this.response + ")");
                }
            }
        };
        getReq.send();
    },

    createBiWeeklyInspection: function (thisProjectTaskId, thisProjectId) {
        var entity = {};
        var getReq = new XMLHttpRequest();
        getReq.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/msdyn_projecttasks(" + thisProjectTaskId + ")?$select=fkh_accessnotes,fkh_lockboxremoved,fkh_mechanicallockbox,fkh_mechanicallockboxnote,fkh_propertygatecode,fkh_rentlylockbox,fkh_rentlylockboxnote,_fkh_taskidentifierid_value,_fkh_unitid_value,msdyn_costestimatecontour,msdyn_effort,msdyn_effortcontour,msdyn_effortestimateatcomplete,msdyn_progress,_msdyn_project_value,msdyn_remaininghours,_msdyn_resourceorganizationalunitid_value,msdyn_resourceutilization,msdyn_salesestimatecontour,msdyn_scheduledend,msdyn_scheduleddurationminutes,msdyn_scheduledhours,msdyn_scheduledstart,msdyn_subject,msdyn_wbsid,_ownerid_value,statuscode", true);
        getReq.setRequestHeader("OData-MaxVersion", "4.0");
        getReq.setRequestHeader("OData-Version", "4.0");
        getReq.setRequestHeader("Accept", "application/json");
        getReq.setRequestHeader("Content-Type", "application/json; charset=utf-8");
        getReq.onreadystatechange = function () {
            if (this.readyState === 4) {
                getReq.onreadystatechange = null;
                if (this.status === 200) {
                    var result = JSON.parse(this.response);
                    var dueDate = new Date();

                    entity.fkh_accessnotes = result["fkh_accessnotes"];
                    entity.fkh_lockboxremoved = result["fkh_lockboxremoved"];
                    entity.fkh_mechanicallockbox = result["fkh_mechanicallockbox"];
                    entity.fkh_mechanicallockboxnote = result["fkh_mechanicallockboxnote"];
                    entity.fkh_propertygatecode = result["fkh_propertygatecode"];
                    entity.fkh_rentlylockbox = result["fkh_rentlylockbox"];
                    entity.fkh_rentlylockboxnote = result["fkh_rentlylockboxnote"];
                    entity.msdyn_costestimatecontour = result["msdyn_costestimatecontour"];
                    entity.msdyn_effort = result["msdyn_effort"];
                    entity.msdyn_effortcontour = result["msdyn_effortcontour"];
                    entity.msdyn_effortestimateatcomplete = result["msdyn_effortestimateatcomplete"];
                    entity.msdyn_progress = 0;
                    entity.msdyn_remaininghours = result["msdyn_remaininghours"];
                    entity.msdyn_resourceutilization = result["msdyn_resourceutilization"];
                    entity.msdyn_salesestimatecontour = result["msdyn_salesestimatecontour"];
                    entity.msdyn_scheduledend = dueDate.addDays(14);
                    entity.msdyn_scheduleddurationminutes = 1440;//result["msdyn_scheduleddurationminutes"];
                    entity.msdyn_scheduledhours = result["msdyn_scheduledhours"];
                    entity.msdyn_scheduledstart = dueDate.addDays(13);
                    entity.msdyn_subject = 'Bi-Weekly Inspection';
                    entity.msdyn_wbsid = result["msdyn_wbsid"] + ".1";
                    entity.statuscode = 1;//Not Started
                    entity["fkh_TaskIdentifierId@odata.bind"] = "/fkh_taskidentifiers(" + result["_fkh_taskidentifierid_value"] + ")";
                    entity["fkh_UnitId@odata.bind"] = "/po_units(" + result["_fkh_unitid_value"] + ")";
                    entity["msdyn_project@odata.bind"] = "/msdyn_projects(" + result["_msdyn_project_value"] + ")";
                    entity["msdyn_ResourceOrganizationalUnitId@odata.bind"] = "/msdyn_organizationalunits(" + result["_msdyn_resourceorganizationalunitid_value"] + ")";
                    entity["ownerid@odata.bind"] = "/systemusers(" + result["_ownerid_value"] + ")";

                    var updateReq = new XMLHttpRequest();
                    updateReq.open("POST", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/msdyn_projecttasks", true);
                    updateReq.setRequestHeader("OData-MaxVersion", "4.0");
                    updateReq.setRequestHeader("OData-Version", "4.0");
                    updateReq.setRequestHeader("Accept", "application/json");
                    updateReq.setRequestHeader("Content-Type", "application/json; charset=utf-8");
                    updateReq.onreadystatechange = function () {
                        if (this.readyState === 4) {
                            updateReq.onreadystatechange = null;
                            if (this.status === 204) {
                                var uri = this.getResponseHeader("OData-EntityId");
                                var regExp = /\(([^)]+)\)/;
                                var matches = regExp.exec(uri);
                                var newEntityId = matches[1];
                                FKH.FieldAndProjectServices.ProjectTaskRibbon.createProjectTaskDependency(thisProjectTaskId, newEntityId, thisProjectId);
                            } else {
                                Xrm.Utility.alertDialog("Error in FKH.FieldAndProjectServices.ProjectTaskRibbon.createBiWeeklyInspection:  " + this.response);
                            }
                        }
                    };
                    updateReq.send(JSON.stringify(entity));
                } else {
                    Xrm.Utility.alertDialog("Error in FKH.FieldAndProjectServices.ProjectTaskRibbon.createBiWeeklyInspection:  " + this.response);
                }
            }
        };
        getReq.send();
    },

    createProjectTaskDependency: function (firstTaskId, secondTaskId, projectId) {
        var entity = {};
        entity.msdyn_linktype = 192350000;
        entity["msdyn_PredecessorTask@odata.bind"] = "/msdyn_projecttasks(" + firstTaskId.toString().replace("{", "").replace("}", "") + ")";
        entity["msdyn_Project@odata.bind"] = "/msdyn_projects(" + projectId.toString().replace("{", "").replace("}", "") + ")";
        entity["msdyn_SuccessorTask@odata.bind"] = "/msdyn_projecttasks(" + secondTaskId.toString().replace("{", "").replace("}", "") + ")";

        var req = new XMLHttpRequest();
        req.open("POST", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/msdyn_projecttaskdependencies", true);
        req.setRequestHeader("OData-MaxVersion", "4.0");
        req.setRequestHeader("OData-Version", "4.0");
        req.setRequestHeader("Accept", "application/json");
        req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
        req.onreadystatechange = function () {
            if (this.readyState === 4) {
                req.onreadystatechange = null;
                if (this.status === 204) {
                    // var uri = this.getResponseHeader("OData-EntityId");
                    // var regExp = /\(([^)]+)\)/;
                    // var matches = regExp.exec(uri);
                    // var newEntityId = matches[1];
                } else {
                    Xrm.Utility.alertDialog("Error in FKH.FieldAndProjectServices.ProjectTaskRibbon.createProjectTaskDependency:  " + this.response);
                }
            }
        };
        req.send(JSON.stringify(entity));
    },

    updateProjectTaskDependency: function (oldPredecessorTaskId, newPredecessorTaskId) {
        var getReq = new XMLHttpRequest();
        getReq.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/msdyn_projecttaskdependencies?$select=_msdyn_predecessortask_value,msdyn_projecttaskdependencyid&$filter=_msdyn_predecessortask_value eq " + oldPredecessorTaskId, true);
        getReq.setRequestHeader("OData-MaxVersion", "4.0");
        getReq.setRequestHeader("OData-Version", "4.0");
        getReq.setRequestHeader("Accept", "application/json");
        getReq.setRequestHeader("Content-Type", "application/json; charset=utf-8");
        getReq.onreadystatechange = function () {
            if (this.readyState === 4) {
                getReq.onreadystatechange = null;
                if (this.status === 200) {
                    var results = JSON.parse(this.response);
                    for (var i = 0; i < results.value.length; i++) {
                        var entity = {};
                        entity["msdyn_predecessortask@odata.bind"] = "/msdyn_projecttasks(" + newPredecessorTaskId + ")";

                        var updateReq = new XMLHttpRequest();
                        updateReq.open("PATCH", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/msdyn_projecttaskdependencies(" + results.value[i]["msdyn_projecttaskdependencyid"] + ")", true);
                        updateReq.setRequestHeader("OData-MaxVersion", "4.0");
                        updateReq.setRequestHeader("OData-Version", "4.0");
                        updateReq.setRequestHeader("Accept", "application/json");
                        updateReq.setRequestHeader("Content-Type", "application/json; charset=utf-8");
                        updateReq.onreadystatechange = function () {
                            if (this.readyState === 4) {
                                updateReq.onreadystatechange = null;
                                if (this.status === 204) {
                                    //Success - No Return Data - Do Something
                                } else {
                                    Xrm.Utility.alertDialog("Error in FKH.FieldAndProjectServices.ProjectTaskRibbon.updateProjectTaskDependency:  " + this.response);
                                }
                            }
                        };
                        updateReq.send(JSON.stringify(entity));
                    }
                } else {
                    Xrm.Utility.alertDialog("Error in FKH.FieldAndProjectServices.ProjectTaskRibbon.updateProjectTaskDependency:  " + this.response);
                }
            }
        };
        getReq.send();
    },

    dateDiffInMinutes: function (startDate, endDate) {
        minutesDiff = Math.floor((endDate - startDate) / (1000 * 60));
        if (minutesDiff < 0) { minutesDiff = 0; }
        return minutesDiff;
    },

    predecessorsAreComplete: function (thisProjectTaskId) {
        predecessorsAreComplete = true;
        if (thisProjectTaskId != null && String(thisProjectTaskId) != "") {
            var req = new XMLHttpRequest();
            req.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/msdyn_projecttaskdependencies?$expand=msdyn_PredecessorTask($select=statuscode)&$filter=_msdyn_successortask_value eq " + thisProjectTaskId, false);
            req.setRequestHeader("OData-MaxVersion", "4.0");
            req.setRequestHeader("OData-Version", "4.0");
            req.setRequestHeader("Accept", "application/json");
            req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
            req.setRequestHeader("Prefer", "odata.include-annotations=\"*\"");
            req.onreadystatechange = function () {
                if (this.readyState === 4) {
                    req.onreadystatechange = null;
                    if (this.status === 200) {
                        var results = JSON.parse(this.response);
                        for (var i = 0; i < results.value.length; i++) {
                            var predecessorStatusReason = results.value[i]["msdyn_PredecessorTask"]["statuscode"];
                            if (predecessorStatusReason != null && predecessorStatusReason != 963850001 /*Completed*/ && predecessorStatusReason != 2 /*Inactive*/) {
                                predecessorsAreComplete = false;
                            }
                        }
                    } else {
                        Xrm.Utility.alertDialog("Error in FKH.FieldAndProjectServices.ProjectTaskRibbon.predecessorsAreComplete:  " + this.response);
                    }
                }
            };
            req.send();
        }
        return predecessorsAreComplete;
    }
};

function Createguid() {
    function s4() {
        return Math.floor((1 + Math.random()) * 0x10000)
            .toString(16)
            .substring(1);
    }
    return s4() + s4() + '-' + s4() + '-' + s4() + '-' + s4() + '-' + s4() + s4() + s4();
};

Date.prototype.addDays = function (days) {
    var date = new Date(this.valueOf());
    date.setDate(date.getDate() + days);
    return date;
};// JavaScript source code
