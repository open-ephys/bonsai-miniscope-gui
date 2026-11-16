import WorkflowContainer from "./workflow.js"

export default {
    defaultTheme: 'light',
    iconLinks: [
        {
            icon: 'github',
            href: 'https://github.com/open-ephys/bonsai-miniscope-gui',
            title: 'GitHub'
        },
        {
            icon: 'bluesky',
            href: 'https://bsky.app/profile/open-ephys.org',
            title: 'Bluesky'
        },
        {
            icon: 'discord',
            href: 'https://discord.gg/WXAx2URNQU',
            title: 'Discord'
        }
    ],
    start: () => {
        WorkflowContainer.init();
    }
}
