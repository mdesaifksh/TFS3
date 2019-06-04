// JavaScript source code
function ProjectTypeOnChange(isOnLoad) {
    var opt1 = { value: 963850000, text: "0 - 1000" };
    var opt2 = { value: 963850003, text: "0 - 2000" };
    var opt3 = { value: 963850001, text: "1000 - 2000" };
    var opt4 = { value: 963850002, text: "2000 - 10000" };
    var opt5 = { value: 963850005, text: ">10000" };
    //debugger;
    var projectType = Xrm.Page.getAttribute("fkh_projecttype").getValue();
    var selectedLevel = Xrm.Page.getAttribute("fkh_level").getValue();
    if (projectType !== null && projectType !== undefined) {
        var pickList = Xrm.Page.getControl("fkh_level");
        var options = Xrm.Page.getAttribute("fkh_level").getOptions();

        if (isOnLoad !== true) {
            Xrm.Page.getAttribute("fkh_level").setValue(null);
            Xrm.Page.getAttribute("fkh_market").setValue(null);
            Xrm.Page.getAttribute("fkh_approver").setValue(null);
        }

        // *** Clear current items
        Xrm.Page.getControl("fkh_level").clearOptions();
        //for (var i = 0; i < options.length; i++) {
        //    pickList.removeOption(options[i].value);
        //}

        if (projectType[0].id.replace('{', '').replace('}', '').toUpperCase() === "23A38E60-C0D0-E811-A96E-000D3A16ACEE")       //Turn Process
        {
            pickList.addOption(opt1);
            pickList.addOption(opt3);
            pickList.addOption(opt4);
            pickList.addOption(opt5);
        }
        else if (projectType[0].id.replace('{', '').replace('}', '').toUpperCase() === "1DD49D5F-D3D6-E811-A96D-000D3A16A650")  //IR
        {
            pickList.addOption(opt2);
            pickList.addOption(opt4);
            pickList.addOption(opt5);
        }

        if (isOnLoad === true && selectedLevel !== null && selectedLevel !== undefined)
            Xrm.Page.getAttribute("fkh_level").setValue(selectedLevel);
    }
}

function onLoad() {
    var formType = Xrm.Page.ui.getFormType();
    if (formType === 1 || formType === 2) {
        ProjectTypeOnChange(true);
    }
}

function BuildName() {
    var name = "";
    var projectType = Xrm.Page.getAttribute("fkh_projecttype").getValue();
    var level = Xrm.Page.getAttribute("fkh_level").getText();
    var market = Xrm.Page.getAttribute("fkh_market").getText();
    //fkh_approver
    var approver = Xrm.Page.getAttribute("fkh_approver").getValue();

    if (projectType !== null && projectType !== undefined)
        name = projectType[0].name;

    if (approver !== null && approver !== undefined) {
        if (name === "")
            name = approver[0].name;
        else
            name += " - " + approver[0].name;
    }

    if (market !== null && market !== undefined) {
        if (name === "")
            name = market;
        else
            name += " - " + market;
    }

    if (level !== null && level !== undefined) {
        if (name === "")
            name = level;
        else
            name += " - " + level;
    }

    if (name !== "")
        Xrm.Page.getAttribute("fkh_name").setValue(name);
}
