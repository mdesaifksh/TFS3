function RequestApproval() {
    //alert('RequestApproval');
    debugger;
    Alert.showLoading();
    var revision = Xrm.Page.getAttribute("fkh_revision").getValue();
    Process.callAction("fkh_RequestForApproval",
        [
            {
                key: "Target",
                type: Process.Type.EntityReference,
                value: { id: Xrm.Page.data.entity.getId().replace('{', '').replace('}', ''), entityType: 'fkh_changeorder' }
            }
            ,
            {
                key: "Revision",
                type: Process.Type.Int,
                value: revision
            },
            {
                key: "ServerUrl",
                type: Process.Type.String,
                value: Xrm.Page.context.getClientUrl()
            }
        ],
        function (params) {
            debugger;
            Alert.hide();
            // Success                      
            if (params.length > 0) {
                if (params[0].key === "IsSuccess" && params[0].value === "true") {
                    Alert.show("Change Order Submitted for Approval!", "Change Order successfully Submitted for Approval.", null, "SUCCESS", 500, 200);
                    Xrm.Page.data.refresh(false).then(function successCallback() {
                        Xrm.Page.ui.refreshRibbon(); Xrm.Page.data.refresh();
                    });
                }
                else {
                    if (params[1].key === "ErrorMessage" && params[1].value !== null && params[1].value !== "" && params[1].value !== undefined)
                        Alert.show("Change Order Submitted for Approval!", params[1].value, null, "ERROR", 500, 200);
                }
            }
            else {
                Alert.show("Change Order Submitted for Approval!", "Change Order successfully Submitted for Approval.", null, "SUCCESS", 500, 200);
                Xrm.Page.data.refresh();
                Xrm.Page.ui.refreshRibbon();
            }
        },
        function (e) {
            // Error
            Alert.show("Error!", e, null, "ERROR", 500, 200);
        }
    );
}


function Approve() {
    //alert('Approve');

    Alert.showLoading();
    var revision = Xrm.Page.getAttribute("fkh_revision").getValue();
    Process.callAction("fkh_ApproveChangeOrder",
        [
            {
                key: "Target",
                type: Process.Type.EntityReference,
                value: { id: Xrm.Page.data.entity.getId().replace('{', '').replace('}', ''), entityType: 'fkh_changeorder' }
            }
            ,
            {
                key: "Revision",
                type: Process.Type.Int,
                value: revision
            },
            {
                key: "ServerUrl",
                type: Process.Type.String,
                value: Xrm.Page.context.getClientUrl()
            }
        ],
        function (params) {
            debugger;
            Alert.hide();
            // Success                      
            if (params.length > 0) {
                if (params[0].key === "IsSuccess" && params[0].value === "true") {
                    Alert.show("Change Order Approval!", "Change Order successfully Approved for this Level.", null, "SUCCESS", 500, 200);
                    Xrm.Page.data.refresh(false).then(function successCallback() {
                        Xrm.Page.ui.refreshRibbon(); Xrm.Page.data.refresh();
                    });
                }
                else {
                    if (params[1].key === "ErrorMessage" && params[1].value !== null && params[1].value !== "" && params[1].value !== undefined)
                        Alert.show("Change Order Approval - Failed!", params[1].value, null, "ERROR", 500, 200);
                }
            }
            else {
                Alert.show("Change Order Approval!", "Change Order successfully Approved.", null, "SUCCESS", 500, 200);
                Xrm.Page.data.refresh();
                Xrm.Page.ui.refreshRibbon();
            }
        },
        function (e) {
            // Error
            Alert.show("Error!", e, null, "ERROR", 500, 200);
        }
    );

    //var options = {
    //    id: "promptExample",
    //    title: "Approve Comments",
    //    message: "Please Provide Approval Comments",
    //    buttons: [new Alert.Button("OK", function (results) {
    //        var details = results[0].value;

    //        alert("Comments: " + details);
    //    })]
    //};

    //var alertBase = new Alert(options);
    //alertBase.showPrompt([
    //    new Alert.MultiLine({ label: "Comments" })
    //]);

}

function CanApproveRejectChangeOrder() {
    var canApproveReject = true;

    var requestor = Xrm.Page.getAttribute('fkh_requestorid').getValue();
    var unit = Xrm.Page.getAttribute('fkh_unitid').getValue();
    if (requestor !== null && requestor !== undefined && unit !== null && unit !== undefined) {
        var userSettings = Xrm.Utility.getGlobalContext().userSettings;
        var userID = userSettings.userId.replace('{', '').replace('}', '').toLowerCase();

        if (userID === requestor[0].id.replace('{', '').replace('}', '').toLowerCase())
            canApproveReject = false;
        else {
            //
            canApproveReject = true;
        }

    }
    else
        canApproveReject = false;

    return canApproveReject;
}

function Reject() {
    //debugger;
    //alert('Reject');
    //var options = { title: "Reject", message: "Reject Approval", icon:"INFO" };
    //new Alert(options).show();
    //fkh_ChangeOrderDescription.html
    Alert.show("Reject", "Please Enter Description", null, "INFO", 500, 200);

    Alert.showWebResource("fkh_ChangeOrderDescription.html", 500, 200, "Reject Comments", [new Alert.Button("OK", function () {
        var iframe_Comments = Alert.getIFrameWindow();
        var comments = iframe_Comments.document.getElementById('txtareaDescription');
        if (comments !== null && comments !== undefined) {
            if (comments.value === null || comments.value === undefined || comments.value === "") {
                //alert('Please Enter Comments before Rejecting Change Order');
                Alert.show("Change Order Rejection!", 'Please Enter Comments before Rejecting Change Order', null, "ERROR", 500, 200);
            }
            else {
                //alert("Comments : " + comments.value);
                //Call Action
                //
                var revision = Xrm.Page.getAttribute("fkh_revision").getValue();
                Process.callAction("fkh_RejectChangeOrder",
                    [
                        {
                            key: "Target",
                            type: Process.Type.EntityReference,
                            value: { id: Xrm.Page.data.entity.getId().replace('{', '').replace('}', ''), entityType: 'fkh_changeorder' }
                        }
                        ,
                        {
                            key: "Revision",
                            type: Process.Type.Int,
                            value: revision
                        },
                        {
                            key: "ServerUrl",
                            type: Process.Type.String,
                            value: Xrm.Page.context.getClientUrl()
                        },
                        {
                            key: "Reason",
                            type: Process.Type.String,
                            value: comments.value
                        }
                    ],
                    function (params) {
                        //debugger;
                        Alert.hide();
                        // Success                      
                        if (params.length > 0) {
                            if (params[0].key === "IsSuccess" && params[0].value === "true") {
                                Alert.show("Change Order Rejection!", "Change Order successfully Rejected.", null, "SUCCESS", 500, 200);
                                Xrm.Page.data.refresh(false).then(function successCallback() {
                                    Xrm.Page.ui.refreshRibbon(); Xrm.Page.data.refresh();
                                });
                            }
                            else {
                                if (params[1].key === "ErrorMessage" && params[1].value !== null && params[1].value !== "" && params[1].value !== undefined)
                                    Alert.show("Change Order Rejection - Failed!", params[1].value, null, "ERROR", 500, 200);
                            }
                        }
                        else {
                            Alert.show("Change Order Rejection!", "Change Order successfully Rejected.", null, "SUCCESS", 500, 200);
                            Xrm.Page.data.refresh();
                            Xrm.Page.ui.refreshRibbon();
                        }
                    },
                    function (e) {
                        // Error
                        Alert.show("Error!", e, null, "ERROR", 500, 200);
                    }
                );
            }
        }
    })]);
}