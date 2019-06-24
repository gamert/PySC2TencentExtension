# Copyright 2017 Google Inc. All Rights Reserved.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#      http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS-IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
"""Module setuptools script."""

from __future__ import absolute_import
from __future__ import division
from __future__ import print_function

from setuptools import setup

description = """PyXS2 - StarCraft II Learning Environment

PyXS is DeepMind's Python component of the StarCraft II Learning Environment
(SC2LE). It exposes Blizzard Entertainment's StarCraft II Machine Learning API
as a Python RL Environment. This is a collaboration between DeepMind and
Blizzard to develop StarCraft II into a rich environment for RL research. PyXS
provides an interface for RL agents to interact with StarCraft 2, getting
observations and sending actions.

We have published an accompanying blogpost and paper
https://deepmind.com/blog/deepmind-and-blizzard-open-starcraft-ii-ai-research-environment/
which outlines our motivation for using StarCraft II for DeepRL research, and
some initial research results using the environment.

Read the README at https://github.com/deepmind/PyXS for more information.
"""

setup(
    name='PyXS2',
    version='2.0.1',
    description='xspace environment and library for training agents.',
    long_description=description,
    author='gamert',
    author_email='gamert@dw.com',
    license='Apache License, Version 2.0',
    keywords='XSpace AI',
    url='https://github.com/gamert/PySC2TencentExtension',
    packages=[
        'pyxs2',
        'pyxs2.agents',
        'pyxs2.bin',
        'pyxs2.env',
        'pyxs2.lib',
        'pyxs2.maps',
        'pyxs2.run_configs',
        'pyxs2.tests',
    ],
    install_requires=[
        'absl-py>=0.1.0',
        'enum34',
        'future',
        'futures; python_version == "2.7"',
        'mock',
        'mpyq',
        'numpy>=1.10',
        'portpicker>=1.2.0',
        'protobuf>=2.6',
        'pygame',
        'requests',
        'xs2clientprotocol>=3.19.0.58400.0',
        'six',
        'sk-video',
        'websocket-client',
        'whichcraft',
    ],
    entry_points={
        'console_scripts': [
            'pyxs2_agent = pyxs2.bin.agent:entry_point',
            'pyxs2_play = pyxs2.bin.play:entry_point',
            'pyxs2_replay_info = pyxs2.bin.replay_info:entry_point',
        ],
    },
    classifiers=[
        'Development Status :: 4 - Beta',
        'Environment :: Console',
        'Intended Audience :: Science/Research',
        'License :: OSI Approved :: Apache Software License',
        'Operating System :: POSIX :: Linux',
        'Operating System :: Microsoft :: Windows',
        'Operating System :: MacOS :: MacOS X',
        'Programming Language :: Python :: 2.7',
        'Programming Language :: Python :: 3.4',
        'Topic :: Scientific/Engineering :: Artificial Intelligence',
    ],
)
