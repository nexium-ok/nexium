/**
 * @param {import('knex')} knex
 */
exports.up = async (knex) => {
    await knex.schema.createTable('moderation_rename_group', (t) => {
        t.bigIncrements('id').notNullable().unsigned();
        t.bigInteger('group_id').notNullable().unsigned();
        t.bigInteger('actor_id').notNullable().unsigned();
        t.string('old_name', 255).notNullable();
        t.string('new_name', 255).notNullable();
        t.dateTime('created_at').notNullable().defaultTo(knex.fn.now());

        t.index(['group_id']);
        t.index(['actor_id']);
    });

    await knex.schema.createTable('moderation_delete_group', (t) => {
        t.bigIncrements('id').notNullable().unsigned();
        t.bigInteger('group_id').notNullable().unsigned();
        t.string('group_name', 255).notNullable();
        t.bigInteger('actor_id').notNullable().unsigned();
        t.dateTime('created_at').notNullable().defaultTo(knex.fn.now());

        t.index(['actor_id']);
    });
};

exports.down = async (knex) => {
    await knex.schema.dropTable('moderation_rename_group');
    await knex.schema.dropTable('moderation_delete_group');
};