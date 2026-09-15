import unittest

import yaml

import pipeline_harness as harness

REPO_ROOT = harness.REPO_ROOT
WORKFLOW = REPO_ROOT / ".github" / "workflows" / "pr-test-bootstrap-command-queue.yml"

class BootstrapCommandQueueWorkflowTests(unittest.TestCase):
    def test_bootstrap_workflow_uses_gcp_metadata_startup_script_not_manual_ssh(self):
        text = WORKFLOW.read_text()
        workflow = yaml.safe_load(text)

        self.assertIn("workflow_dispatch", workflow["on"])
        # Authenticating is the claim; the pair moved behind a composite action,
        # and test_gcp_session_consistency.py is what holds that one place honest.
        self.assertIn("./.github/actions/gcp-session", text)
        # Not the whole command: the upload also sets -h Content-Type, without which
        # the agent fetches every script as a byte[] and refreshes none of them
        # (test_command_queue_self_update.ContentTypeOfPublishedScriptsTests). Pinning
        # the flag order here would break that fix rather than notice it.
        self.assertIn("Deployment/PrTestEnvironments/*.ps1", text)
        self.assertIn("gsutil", text)
        # Resolving the VM by its address is the claim; the lookup moved behind a
        # composite action, and Pester/ResolveVmTarget.Tests.ps1 is what holds that
        # one place honest. It had to move: this workflow and the diagnose run
        # spelled the same gcloud filter two different ways, and only the
        # certificate renewal checked that anything had been resolved.
        self.assertIn("./.github/actions/resolve-vm", text)
        self.assertIn("GCP_VM_EXTERNAL_IP", text)
        # Staging and rebooting are the claim; both moved behind composite actions,
        # and Pester/VmStartupScript.Tests.ps1 and Pester/RestartVm.Tests.ps1 are
        # what hold those two places honest. The metadata key and the stop/start
        # pair live there now -- asserting the strings here would pin the call
        # sites of code this file no longer contains.
        self.assertIn("./.github/actions/inject-startup-script", text)
        self.assertIn("./.github/actions/restart-vm", text)
        # The scope list stays asserted here because it stays this workflow's
        # decision: restart-vm applies whatever it is handed, and the fleet is the
        # caller that replaces rather than unions. See ADR-0006 for the same split.
        self.assertIn("https://www.googleapis.com/auth/cloud-platform", text)
        self.assertIn("Install-PrEnvironmentCommandQueueTask.ps1", text)
        self.assertIn("Invoke-PrEnvironmentCommandQueue.ps1", text)
        self.assertIn("PR_TEST_GCS_BUCKET", text)
        self.assertIn("rock-pr-env-{0}-{1}", text)
        self.assertNotIn("sshpass", text)
        self.assertNotIn("scp ", text)

if __name__ == "__main__":
    unittest.main()
